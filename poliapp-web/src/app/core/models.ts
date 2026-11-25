export interface Sesion {
  token: string;
  expira: string;
  id: number;
  nombre: string;
  correo: string;
  rol: 'Deportista' | 'Administrador';
  complejoId: number | null;
}

export interface Cancha {
  id: number;
  complejoId: number;
  complejoNombre: string;
  distrito: string;
  nombre: string;
  tipo: string;
  precioHora: number;
  horaApertura: string;
  horaCierre: string;
  activa: boolean;
}

export interface Bloque { hora: string; libre: boolean; }
export interface Disponibilidad { cancha: Cancha; bloques: Bloque[]; }

export type EstadoReserva = 'Pendiente' | 'Confirmada' | 'Cancelada' | 'Rechazada' | 'Finalizada';

export interface Reserva {
  id: number;
  codigo: string;
  canchaId: number;
  canchaNombre: string;
  complejoNombre: string;
  clienteNombre: string;
  clienteTelefono: string;
  fecha: string;
  horaInicio: string;
  horaFin: string;
  montoTotal: number;
  estado: EstadoReserva;
  motivoRechazo: string | null;
  puedeModificarse: boolean;
  estadoPago: 'PorValidar' | 'Validado' | 'Observado' | null;
}

export interface Complejo { id: number; nombre: string; direccion: string; distrito: string; telefono: string | null; canchas: number; }

export interface Pago {
  id: number; reservaId: number; codigoReserva: string; cliente: string; cancha: string; fecha: string;
  metodo: string; numeroOperacion: string; monto: number; estado: 'PorValidar' | 'Validado' | 'Observado'; fechaRegistro: string;
}

export interface Panel { reservasHoy: number; pendientesPorAtender: number; pagosPorValidar: number; ingresosMes: number; ocupacionSemana: number; }

export interface Reporte {
  desde: string; hasta: string; totalReservas: number; totalIngresos: number; ocupacionPromedio: number;
  porCancha: { canchaId: number; cancha: string; reservas: number; horasReservadas: number; horasDisponibles: number; ocupacion: number; ingresos: number }[];
  porDia: { fecha: string; ingresos: number; reservas: number }[];
}

/** Fecha local en formato yyyy-MM-dd (evita el desfase de toISOString en UTC). */
export function fechaLocal(d = new Date()): string {
  const p = (n: number) => String(n).padStart(2, '0');
  return `${d.getFullYear()}-${p(d.getMonth() + 1)}-${p(d.getDate())}`;
}

export function sumarHora(hora: string, horas: number): string {
  const h = +hora.slice(0, 2) + horas;
  return `${String(h).padStart(2, '0')}:${hora.slice(3, 5)}`;
}

export function mensajeError(err: any, porDefecto = 'Ocurrió un error inesperado.'): string {
  const e = err?.error;
  if (e?.errors) return Object.values(e.errors).flat().join(' ');
  return e?.detail ?? e?.title ?? (err?.status === 0 ? 'No se pudo conectar con la API. ¿Está corriendo PoliApp.Api?' : porDefecto);
}

export const COLOR_ESTADO: Record<string, string> = {
  Pendiente: 'text-bg-warning', Confirmada: 'text-bg-success', Cancelada: 'text-bg-danger',
  Rechazada: 'text-bg-danger', Finalizada: 'text-bg-info', PorValidar: 'text-bg-warning',
  Validado: 'text-bg-success', Observado: 'text-bg-danger',
};
