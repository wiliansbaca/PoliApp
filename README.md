# Poli-App

Plataforma web para la gestión de reservas de canchas sintéticas en Cusco (tesis XP + SDD).

| Capa | Tecnología |
|---|---|
| Backend | ASP.NET Core Web API (.NET 10), EF Core 10, JWT |
| Base de datos | SQL Server 2025 — `PoliAppDB` (instancia `localhost\MSSQLSERVER01`) |
| Frontend | Angular 22 + Bootstrap 5 |
| Pruebas | xUnit (dominio) · Vitest (Angular) |

## Historias de usuario implementadas
| HU | Funcionalidad | Dónde |
|---|---|---|
| HU01 | Registro e inicio de sesión con JWT | `/login` · `AuthController` |
| HU02 | Gestión de complejos y canchas | `/admin/complejos` |
| HU03 | Horarios de atención y tarifas | `/admin/horarios` |
| HU04 | Consulta de disponibilidad | `/canchas` |
| HU05 | Reserva con validación de cruce de horario | `/reservar` |
| HU06 | Mis reservas: cancelar y reprogramar (hasta 6 h antes) | `/mis-reservas` |
| HU07 | Panel de reservas por atender (confirmar / rechazar) | `/admin/reservas` |
| HU08 | Correo de confirmación (SMTP; en desarrollo se registra en el log) | `NotificacionService` |
| HU09 | Reportes de ocupación e ingresos | `/admin/reportes` |
| HU10 | Vista móvil (diseño responsivo con Bootstrap) | todas |
| HU11 | Pago en línea con Yape/Plin (registro y validación) | `/mis-reservas`, `/admin/pagos` |

Pendiente: sincronización con Google Calendar (campo `Complejos.GoogleCalendarId` ya previsto) y despliegue.

## Cómo ejecutar
1. **Base de datos**: en SSMS ejecuta `database/01_CrearBaseDatos.sql` (recrea las tablas).
2. **API**: abre `PoliApp.slnx` en Visual Studio y pulsa ▶ (perfil `https`). En el primer arranque se crean los usuarios
   y reservas de prueba. Documentación en `https://localhost:7180/scalar`.
3. **Frontend**: en `poliapp-web` ejecuta `npm install` y luego `npm start`; abre `http://localhost:4200`.

Cuentas de prueba: `admin@poliapp.pe` / `Admin123*` (administrador del Complejo Valencia) · `jperez@gmail.com` / `Juan123*` (deportista).

## Pruebas
- Backend: Visual Studio → Prueba → Ejecutar todas las pruebas (proyecto `PoliApp.Tests`).
- Frontend: `npm test`.
