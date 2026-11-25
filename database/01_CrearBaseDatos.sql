/* =========================================================
   Poli-App - Base de datos (SQL Server 2025)
   Plataforma web de reservas de canchas sintéticas - Cusco
   Ejecutar en SSMS conectado a la instancia local.
   Los usuarios y reservas de prueba los crea la API al iniciar
   en modo Development (DbSeeder), con contraseñas cifradas.
   ========================================================= */
IF DB_ID(N'PoliAppDB') IS NULL
    CREATE DATABASE PoliAppDB;
GO

USE PoliAppDB;
GO

IF OBJECT_ID(N'dbo.Pagos',    N'U') IS NOT NULL DROP TABLE dbo.Pagos;
IF OBJECT_ID(N'dbo.Reservas', N'U') IS NOT NULL DROP TABLE dbo.Reservas;
IF OBJECT_ID(N'dbo.Usuarios', N'U') IS NOT NULL DROP TABLE dbo.Usuarios;
IF OBJECT_ID(N'dbo.Canchas',  N'U') IS NOT NULL DROP TABLE dbo.Canchas;
IF OBJECT_ID(N'dbo.Complejos',N'U') IS NOT NULL DROP TABLE dbo.Complejos;
GO

CREATE TABLE dbo.Complejos (
    Id               INT IDENTITY(1,1) CONSTRAINT PK_Complejos PRIMARY KEY,
    Nombre           NVARCHAR(120) NOT NULL,
    Direccion        NVARCHAR(200) NOT NULL,
    Distrito         NVARCHAR(60)  NOT NULL,
    Telefono         NVARCHAR(15)  NULL,
    GoogleCalendarId NVARCHAR(200) NULL,
    FechaRegistro    DATETIME2     NOT NULL CONSTRAINT DF_Complejos_FechaRegistro DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dbo.Canchas (
    Id           INT IDENTITY(1,1) CONSTRAINT PK_Canchas PRIMARY KEY,
    ComplejoId   INT            NOT NULL CONSTRAINT FK_Canchas_Complejos REFERENCES dbo.Complejos(Id),
    Nombre       NVARCHAR(80)   NOT NULL,
    Tipo         NVARCHAR(40)   NOT NULL,
    PrecioHora   DECIMAL(10,2)  NOT NULL CONSTRAINT CK_Canchas_Precio CHECK (PrecioHora > 0),
    HoraApertura TIME(7)        NOT NULL CONSTRAINT DF_Canchas_Apertura DEFAULT '07:00',
    HoraCierre   TIME(7)        NOT NULL CONSTRAINT DF_Canchas_Cierre DEFAULT '23:00',
    Activa       BIT            NOT NULL CONSTRAINT DF_Canchas_Activa DEFAULT 1,
    ICalOcupadosUrl NVARCHAR(500) NULL,   -- dirección secreta iCal de Google Calendar (horarios bloqueados)
    CalendarioToken NVARCHAR(32)  NULL,   -- código del enlace .ics de reservas para suscribirse
    CONSTRAINT CK_Canchas_Horario CHECK (HoraCierre > HoraApertura)
);

CREATE TABLE dbo.Usuarios (
    Id            INT IDENTITY(1,1) CONSTRAINT PK_Usuarios PRIMARY KEY,
    Nombres       NVARCHAR(80)  NOT NULL,
    Apellidos     NVARCHAR(80)  NOT NULL,
    Correo        NVARCHAR(120) NOT NULL CONSTRAINT UQ_Usuarios_Correo UNIQUE,
    Celular       NVARCHAR(15)  NOT NULL,
    PasswordHash  NVARCHAR(200) NOT NULL,
    Rol           TINYINT       NOT NULL CONSTRAINT DF_Usuarios_Rol DEFAULT 0, -- 0 Deportista, 1 Administrador
    ComplejoId    INT           NULL CONSTRAINT FK_Usuarios_Complejos REFERENCES dbo.Complejos(Id),
    FechaRegistro DATETIME2     NOT NULL CONSTRAINT DF_Usuarios_FechaRegistro DEFAULT SYSUTCDATETIME()
);

CREATE TABLE dbo.Reservas (
    Id              INT IDENTITY(1,1) CONSTRAINT PK_Reservas PRIMARY KEY,
    CanchaId        INT           NOT NULL CONSTRAINT FK_Reservas_Canchas REFERENCES dbo.Canchas(Id),
    UsuarioId       INT           NULL CONSTRAINT FK_Reservas_Usuarios REFERENCES dbo.Usuarios(Id),
    ClienteNombre   NVARCHAR(100) NOT NULL,
    ClienteTelefono NVARCHAR(15)  NOT NULL,
    Fecha           DATE          NOT NULL,
    HoraInicio      TIME(7)       NOT NULL,
    HoraFin         TIME(7)       NOT NULL,
    MontoTotal      DECIMAL(10,2) NOT NULL,
    Estado          TINYINT       NOT NULL CONSTRAINT DF_Reservas_Estado DEFAULT 0, -- 0 Pendiente, 1 Confirmada, 2 Cancelada, 3 Rechazada, 4 Finalizada
    MotivoRechazo   NVARCHAR(200) NULL,
    FechaRegistro   DATETIME2     NOT NULL CONSTRAINT DF_Reservas_FechaRegistro DEFAULT SYSUTCDATETIME(),
    CONSTRAINT CK_Reservas_Horas CHECK (HoraFin > HoraInicio)
);
CREATE INDEX IX_Reservas_CanchaId_Fecha ON dbo.Reservas (CanchaId, Fecha);
CREATE INDEX IX_Reservas_UsuarioId ON dbo.Reservas (UsuarioId);

CREATE TABLE dbo.Pagos (
    Id              INT IDENTITY(1,1) CONSTRAINT PK_Pagos PRIMARY KEY,
    ReservaId       INT           NOT NULL CONSTRAINT FK_Pagos_Reservas REFERENCES dbo.Reservas(Id),
    Metodo          NVARCHAR(10)  NOT NULL CONSTRAINT CK_Pagos_Metodo CHECK (Metodo IN (N'Yape', N'Plin')),
    NumeroOperacion NVARCHAR(30)  NOT NULL,
    Monto           DECIMAL(10,2) NOT NULL,
    Estado          TINYINT       NOT NULL CONSTRAINT DF_Pagos_Estado DEFAULT 0, -- 0 PorValidar, 1 Validado, 2 Observado
    FechaRegistro   DATETIME2     NOT NULL CONSTRAINT DF_Pagos_FechaRegistro DEFAULT SYSUTCDATETIME()
);
GO

/* Datos de prueba: complejos y canchas */
INSERT INTO dbo.Complejos (Nombre, Direccion, Distrito, Telefono) VALUES
 (N'Complejo Deportivo Valencia', N'Av. de la Cultura s/n', N'Wanchaq', N'984000000'),
 (N'Camino Real',                 N'Urb. Camino Real B-4',  N'San Sebastián', N'984111222'),
 (N'Coripata Sur',                N'Av. Velasco Astete 120', N'Wanchaq', N'984333444');

INSERT INTO dbo.Canchas (ComplejoId, Nombre, Tipo, PrecioHora, HoraApertura, HoraCierre) VALUES
 (1, N'Cancha 1', N'Fútbol 7', 70.00, '07:00', '23:00'),
 (1, N'Cancha 2', N'Fútbol 7', 70.00, '07:00', '23:00'),
 (2, N'Cancha 1', N'Fútbol 6', 60.00, '08:00', '23:00'),
 (2, N'Cancha 2', N'Fútbol 8', 80.00, '08:00', '23:00'),
 (3, N'Cancha 1', N'Fútbol 7', 65.00, '09:00', '22:00');
GO

SELECT c.Id, x.Nombre AS Complejo, c.Nombre, c.Tipo, c.PrecioHora, c.HoraApertura, c.HoraCierre
FROM dbo.Canchas c JOIN dbo.Complejos x ON x.Id = c.ComplejoId;
