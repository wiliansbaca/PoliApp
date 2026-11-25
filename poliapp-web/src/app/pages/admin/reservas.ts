import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { COLOR_ESTADO, mensajeError, Reserva } from '../../core/models';

/** HU07: panel de reservas por atender (confirmar o rechazar). */
@Component({
  selector: 'app-reservas-admin',
  imports: [FormsModule, CurrencyPipe, DatePipe],
  template: `
    <div class="d-flex flex-wrap justify-content-between align-items-center mb-3">
      <h3 class="mb-0">Reservas por atender</h3>
      <select class="form-select w-auto" [ngModel]="estado()" (ngModelChange)="estado.set($event); cargar()">
        <option value="Pendiente">Pendientes</option>
        <option value="Confirmada">Confirmadas</option>
        <option value="">Todas (desde hoy)</option>
      </select>
    </div>
    @if (mensaje()) { <div class="alert py-2" [class.alert-success]="ok()" [class.alert-danger]="!ok()">{{ mensaje() }}</div> }
    <div class="card p-3">
      <div class="table-responsive">
        <table class="table table-sm align-middle mb-0 text-nowrap">
          <thead class="table-light"><tr><th>Código</th><th>Cliente</th><th>Celular</th><th>Cancha</th><th>Fecha</th><th>Horario</th><th>Monto</th><th>Estado</th><th>Pago</th><th class="text-end">Acción</th></tr></thead>
          <tbody>
            @for (r of reservas(); track r.id) {
              <tr>
                <td class="fw-semibold">{{ r.codigo }}</td>
                <td>{{ r.clienteNombre }}</td>
                <td>{{ r.clienteTelefono }}</td>
                <td>{{ r.canchaNombre }}</td>
                <td>{{ r.fecha + 'T00:00' | date: 'dd/MM/yyyy' }}</td>
                <td>{{ r.horaInicio }} – {{ r.horaFin }}</td>
                <td>{{ r.montoTotal | currency: 'PEN' : 'S/ ' }}</td>
                <td><span class="badge" [class]="color[r.estado]">{{ r.estado }}</span></td>
                <td>@if (r.estadoPago) { <span class="badge" [class]="color[r.estadoPago]">{{ r.estadoPago === 'PorValidar' ? 'Por validar' : r.estadoPago }}</span> } @else { — }</td>
                <td class="text-end text-nowrap">
                  @if (r.estado === 'Pendiente') {
                    <button class="btn btn-sm btn-success me-1" (click)="confirmar(r)">Confirmar</button>
                    <button class="btn btn-sm btn-danger" (click)="rechazar(r)">Rechazar</button>
                  } @else { — }
                </td>
              </tr>
            } @empty {
              <tr><td colspan="10" class="text-center text-muted py-4">No hay reservas con ese estado.</td></tr>
            }
          </tbody>
        </table>
      </div>
    </div>
  `,
})
export class ReservasAdmin {
  private readonly api = inject(ApiService);
  protected readonly color = COLOR_ESTADO;
  protected readonly estado = signal('Pendiente');
  protected readonly reservas = signal<Reserva[]>([]);
  protected readonly mensaje = signal('');
  protected readonly ok = signal(true);

  constructor() { this.cargar(); }

  protected cargar() { this.api.reservasAdmin(this.estado()).subscribe(r => this.reservas.set(r)); }

  private aplicar(r: Reserva, texto: string) {
    this.reservas.update(l => l.map(x => (x.id === r.id ? r : x)));
    this.mensaje.set(texto);
    this.ok.set(true);
  }

  protected confirmar(r: Reserva) {
    this.api.confirmar(r.id).subscribe({
      next: x => this.aplicar(x, `Reserva ${x.codigo} confirmada.`),
      error: e => { this.mensaje.set(mensajeError(e)); this.ok.set(false); },
    });
  }

  protected rechazar(r: Reserva) {
    const motivo = prompt(`Motivo del rechazo de ${r.codigo} (opcional):`) ?? '';
    this.api.rechazar(r.id, motivo).subscribe({
      next: x => this.aplicar(x, `Reserva ${x.codigo} rechazada.`),
      error: e => { this.mensaje.set(mensajeError(e)); this.ok.set(false); },
    });
  }
}
