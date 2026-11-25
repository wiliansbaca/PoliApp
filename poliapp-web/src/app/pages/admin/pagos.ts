import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { COLOR_ESTADO, mensajeError, Pago } from '../../core/models';

/** HU11: validación de pagos con Yape/Plin. */
@Component({
  selector: 'app-pagos-admin',
  imports: [FormsModule, CurrencyPipe, DatePipe],
  template: `
    <div class="d-flex flex-wrap justify-content-between align-items-center mb-3">
      <h3 class="mb-0">Pagos en línea</h3>
      <select class="form-select w-auto" [ngModel]="estado()" (ngModelChange)="estado.set($event); cargar()">
        <option value="PorValidar">Por validar</option>
        <option value="">Todos</option>
      </select>
    </div>
    @if (mensaje()) { <div class="alert py-2" [class.alert-success]="ok()" [class.alert-danger]="!ok()">{{ mensaje() }}</div> }
    <div class="card p-3">
      <table class="table align-middle mb-0">
        <thead class="table-light"><tr><th>Reserva</th><th>Cliente</th><th>Cancha</th><th>Fecha turno</th><th>Método</th><th>N.° operación</th><th>Monto</th><th>Estado</th><th class="text-end">Acción</th></tr></thead>
        <tbody>
          @for (p of pagos(); track p.id) {
            <tr>
              <td class="fw-semibold">{{ p.codigoReserva }}</td><td>{{ p.cliente }}</td><td>{{ p.cancha }}</td>
              <td>{{ p.fecha + 'T00:00' | date: 'dd/MM/yyyy' }}</td>
              <td><span class="badge" [class]="p.metodo === 'Yape' ? 'text-bg-primary' : 'text-bg-info'">{{ p.metodo }}</span></td>
              <td>{{ p.numeroOperacion }}</td><td>{{ p.monto | currency: 'PEN' : 'S/ ' }}</td>
              <td><span class="badge" [class]="color[p.estado]">{{ p.estado === 'PorValidar' ? 'Por validar' : p.estado }}</span></td>
              <td class="text-end text-nowrap">
                @if (p.estado === 'PorValidar') {
                  <button class="btn btn-sm btn-success me-1" (click)="validar(p)">Validar</button>
                  <button class="btn btn-sm btn-outline-danger" (click)="observar(p)">Observar</button>
                } @else { — }
              </td>
            </tr>
          } @empty {
            <tr><td colspan="9" class="text-center text-muted py-4">No hay pagos en este estado.</td></tr>
          }
        </tbody>
      </table>
    </div>
  `,
})
export class PagosAdmin {
  private readonly api = inject(ApiService);
  protected readonly color = COLOR_ESTADO;
  protected readonly estado = signal('PorValidar');
  protected readonly pagos = signal<Pago[]>([]);
  protected readonly mensaje = signal('');
  protected readonly ok = signal(true);

  constructor() { this.cargar(); }

  protected cargar() { this.api.pagos(this.estado()).subscribe(p => this.pagos.set(p)); }

  protected validar(p: Pago) {
    this.api.validarPago(p.id).subscribe({
      next: () => { this.mensaje.set(`Pago de ${p.codigoReserva} validado; la reserva quedó confirmada.`); this.ok.set(true); this.cargar(); },
      error: e => { this.mensaje.set(mensajeError(e)); this.ok.set(false); },
    });
  }

  protected observar(p: Pago) {
    this.api.observarPago(p.id).subscribe({
      next: () => { this.mensaje.set(`Pago de ${p.codigoReserva} observado.`); this.ok.set(true); this.cargar(); },
      error: e => { this.mensaje.set(mensajeError(e)); this.ok.set(false); },
    });
  }
}
