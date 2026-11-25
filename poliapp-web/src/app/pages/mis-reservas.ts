import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../core/api.service';
import { COLOR_ESTADO, fechaLocal, mensajeError, Reserva, sumarHora } from '../core/models';

/** HU06 y HU11: mis reservas, cancelar, reprogramar y pagar con Yape/Plin. */
@Component({
  selector: 'app-mis-reservas',
  imports: [FormsModule, CurrencyPipe, DatePipe],
  template: `
    <div class="container py-4">
      <h3 class="mb-3">Mis reservas</h3>
      @if (mensaje()) { <div class="alert py-2" [class.alert-success]="ok()" [class.alert-danger]="!ok()">{{ mensaje() }}</div> }
      @if (reprogramando(); as r) {
        <div class="card p-4 mb-3 border border-warning">
          <h5>Reprogramar {{ r.codigo }}</h5>
          <div class="row g-2 align-items-end">
            <div class="col-md-4"><label class="form-label" for="nf">Nueva fecha</label>
              <input id="nf" type="date" class="form-control" [min]="hoy" [(ngModel)]="nuevaFecha" /></div>
            <div class="col-md-3"><label class="form-label" for="nh">Hora de inicio</label>
              <input id="nh" type="time" step="3600" class="form-control" [(ngModel)]="nuevaHora" /></div>
            <div class="col-md-5">
              <button class="btn btn-primary me-2" (click)="reprogramar(r)">Guardar</button>
              <button class="btn btn-secondary" (click)="reprogramando.set(null)">Cerrar</button>
            </div>
          </div>
        </div>
      }

      @if (pagando(); as r) {
        <div class="card p-4 mb-3 border border-success">
          <h5><i class="bi bi-phone me-1"></i>Pago en línea de {{ r.codigo }}</h5>
          <p class="mb-2">Yapea o plinea <b>{{ r.montoTotal | currency: 'PEN' : 'S/ ' }}</b> al <b>984 000 000</b> ({{ r.complejoNombre }}) y registra el número de operación.</p>
          <div class="row g-2 align-items-end">
            <div class="col-md-3"><label class="form-label" for="me">Método</label>
              <select id="me" class="form-select" [(ngModel)]="metodo"><option>Yape</option><option>Plin</option></select></div>
            <div class="col-md-4"><label class="form-label" for="op">N.° de operación</label>
              <input id="op" class="form-control" [(ngModel)]="operacion" placeholder="Ej. 12345678" /></div>
            <div class="col-md-5">
              <button class="btn btn-primary me-2" (click)="pagar(r)">Registrar pago</button>
              <button class="btn btn-secondary" (click)="pagando.set(null)">Cerrar</button>
            </div>
          </div>
        </div>
      }
      <div class="card p-3">
        <div class="table-responsive">
          <table class="table align-middle mb-0">
            <thead class="table-light"><tr><th>Código</th><th>Complejo / cancha</th><th>Fecha</th><th>Horario</th><th>Monto</th><th>Estado</th><th>Pago</th><th class="text-end">Acciones</th></tr></thead>
            <tbody>
              @for (r of reservas(); track r.id) {
                <tr>
                  <td class="fw-semibold">{{ r.codigo }}</td>
                  <td>{{ r.complejoNombre }} · {{ r.canchaNombre }}</td>
                  <td>{{ r.fecha + 'T00:00' | date: 'dd/MM/yyyy' }}</td>
                  <td>{{ r.horaInicio }} – {{ r.horaFin }}</td>
                  <td>{{ r.montoTotal | currency: 'PEN' : 'S/ ' }}</td>
                  <td><span class="badge" [class]="color[r.estado]">{{ r.estado }}</span></td>
                  <td>
                    @if (r.estadoPago) { <span class="badge" [class]="color[r.estadoPago]">{{ etiquetaPago(r.estadoPago) }}</span> }
                    @else if (r.estado === 'Pendiente' || r.estado === 'Confirmada') {
                      <button class="btn btn-sm btn-outline-primary" (click)="abrirPago(r)"><i class="bi bi-phone"></i> Pagar</button>
                    } @else { — }
                  </td>
                  <td class="text-end text-nowrap">
                    @if (r.puedeModificarse) {
                      <button class="btn btn-sm btn-warning me-1" (click)="abrirReprogramar(r)">Reprogramar</button>
                      <button class="btn btn-sm btn-danger" (click)="cancelar(r)">Cancelar</button>
                    } @else { — }
                  </td>
                </tr>
              } @empty {
                <tr><td colspan="8" class="text-muted text-center py-4">Aún no tienes reservas.</td></tr>
              }
            </tbody>
          </table>
        </div>
        <p class="small text-muted mt-2 mb-0">Las cancelaciones y reprogramaciones se permiten hasta 6 horas antes del inicio del turno.</p>
      </div>

    </div>
  `,
})
export class MisReservas {
  private readonly api = inject(ApiService);
  protected readonly color = COLOR_ESTADO;
  protected readonly hoy = fechaLocal();
  protected readonly reservas = signal<Reserva[]>([]);
  protected readonly mensaje = signal('');
  protected readonly ok = signal(true);
  protected readonly reprogramando = signal<Reserva | null>(null);
  protected readonly pagando = signal<Reserva | null>(null);
  protected nuevaFecha = '';
  protected nuevaHora = '';
  protected metodo = 'Yape';
  protected operacion = '';

  constructor() { this.cargar(); }

  private cargar() {
    this.api.misReservas().subscribe({ next: r => this.reservas.set(r), error: e => this.aviso(mensajeError(e), false) });
  }

  private actualizar(r: Reserva, texto: string) {
    this.reservas.update(l => l.map(x => (x.id === r.id ? r : x)));
    this.aviso(texto, true);
  }

  private aviso(texto: string, ok: boolean) { this.mensaje.set(texto); this.ok.set(ok); }

  protected etiquetaPago(e: string) { return e === 'PorValidar' ? 'Por validar' : e; }

  protected cancelar(r: Reserva) {
    if (!confirm(`¿Cancelar la reserva ${r.codigo}?`)) return;
    this.api.cancelar(r.id).subscribe({ next: x => this.actualizar(x, `Reserva ${x.codigo} cancelada.`), error: e => this.aviso(mensajeError(e), false) });
  }

  protected abrirReprogramar(r: Reserva) {
    this.pagando.set(null);
    this.nuevaFecha = r.fecha;
    this.nuevaHora = r.horaInicio;
    this.reprogramando.set(r);
  }

  protected reprogramar(r: Reserva) {
    const horas = +r.horaFin.slice(0, 2) - +r.horaInicio.slice(0, 2);
    const ini = this.nuevaHora.slice(0, 5);
    this.api.reprogramar(r.id, { fecha: this.nuevaFecha, horaInicio: ini, horaFin: sumarHora(ini, horas) }).subscribe({
      next: x => { this.reprogramando.set(null); this.actualizar(x, `Reserva ${x.codigo} reprogramada; queda pendiente de confirmación.`); },
      error: e => this.aviso(mensajeError(e), false),
    });
  }

  protected abrirPago(r: Reserva) {
    this.reprogramando.set(null);
    this.operacion = '';
    this.pagando.set(r);
  }

  protected pagar(r: Reserva) {
    this.api.pagar(r.id, { metodo: this.metodo, numeroOperacion: this.operacion.trim() }).subscribe({
      next: x => { this.pagando.set(null); this.actualizar(x, 'Pago registrado. El administrador lo validará en breve.'); },
      error: e => this.aviso(mensajeError(e), false),
    });
  }
}
