import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Cancha, Complejo, Disponibilidad, Pago, Panel, Reporte, Reserva } from './models';

type Params = Record<string, string | number>;

function limpiar(p: Record<string, string | number | null | undefined>): Params {
  return Object.fromEntries(Object.entries(p).filter(([, v]) => v !== null && v !== undefined && v !== '')) as Params;
}

@Injectable({ providedIn: 'root' })
export class ApiService {
  private readonly http = inject(HttpClient);

  // Público (HU04)
  filtros() { return this.http.get<{ tipos: string[]; distritos: string[] }>('/api/canchas/filtros'); }
  cancha(id: number) { return this.http.get<Cancha>(`/api/canchas/${id}`); }
  disponibilidad(fecha: string, tipo?: string, distrito?: string, canchaId?: number) {
    return this.http.get<Disponibilidad[]>('/api/canchas/disponibilidad', { params: limpiar({ fecha, tipo, distrito, canchaId }) });
  }

  // Deportista (HU05, HU06, HU11)
  misReservas() { return this.http.get<Reserva[]>('/api/reservas/mias'); }
  reservar(dto: { canchaId: number; fecha: string; horaInicio: string; horaFin: string }) {
    return this.http.post<Reserva>('/api/reservas', dto);
  }
  cancelar(id: number) { return this.http.patch<Reserva>(`/api/reservas/${id}/cancelar`, {}); }
  reprogramar(id: number, dto: { fecha: string; horaInicio: string; horaFin: string }) {
    return this.http.patch<Reserva>(`/api/reservas/${id}/reprogramar`, dto);
  }
  pagar(id: number, dto: { metodo: string; numeroOperacion: string }) {
    return this.http.post<Reserva>(`/api/reservas/${id}/pago`, dto);
  }

  // Administrador (HU02, HU03, HU07, HU09, HU11)
  panel() { return this.http.get<Panel>('/api/admin/panel'); }
  complejos() { return this.http.get<Complejo[]>('/api/admin/complejos'); }
  editarComplejo(id: number, dto: Partial<Complejo>) { return this.http.put<Complejo>(`/api/admin/complejos/${id}`, dto); }
  canchasAdmin() { return this.http.get<Cancha[]>('/api/admin/canchas'); }
  crearCancha(dto: Partial<Cancha>) { return this.http.post<Cancha>('/api/admin/canchas', dto); }
  editarCancha(id: number, dto: Partial<Cancha>) { return this.http.put<Cancha>(`/api/admin/canchas/${id}`, dto); }
  horario(id: number, dto: { horaApertura: string; horaCierre: string; precioHora: number }) {
    return this.http.put<Cancha>(`/api/admin/canchas/${id}/horario`, dto);
  }
  reservasAdmin(estado?: string, desde?: string) {
    return this.http.get<Reserva[]>('/api/admin/reservas', { params: limpiar({ estado, desde }) });
  }
  confirmar(id: number) { return this.http.patch<Reserva>(`/api/admin/reservas/${id}/confirmar`, {}); }
  rechazar(id: number, motivo: string) { return this.http.patch<Reserva>(`/api/admin/reservas/${id}/rechazar`, { motivo }); }
  pagos(estado?: string) { return this.http.get<Pago[]>('/api/admin/pagos', { params: limpiar({ estado }) }); }
  validarPago(id: number) { return this.http.patch<void>(`/api/admin/pagos/${id}/validar`, {}); }
  observarPago(id: number) { return this.http.patch<void>(`/api/admin/pagos/${id}/observar`, {}); }
  reporte(desde: string, hasta: string) { return this.http.get<Reporte>('/api/admin/reportes', { params: { desde, hasta } }); }
}
