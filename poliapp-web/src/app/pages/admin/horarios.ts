import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { Cancha, mensajeError } from '../../core/models';

/** HU03: configuración de horarios de atención y tarifas por cancha. */
@Component({
  selector: 'app-horarios-admin',
  imports: [FormsModule],
  template: `
    <h3 class="mb-3">Horarios y tarifas</h3>
    @if (mensaje()) { <div class="alert py-2" [class.alert-success]="ok()" [class.alert-danger]="!ok()">{{ mensaje() }}</div> }
    <div class="card p-4">
      <table class="table align-middle mb-0">
        <thead class="table-light"><tr><th>Cancha</th><th>Tipo</th><th style="width:150px">Apertura</th><th style="width:150px">Cierre</th><th style="width:150px">Tarifa por hora (S/)</th><th></th></tr></thead>
        <tbody>
          @for (c of canchas(); track c.id) {
            <tr>
              <td class="fw-semibold">{{ c.nombre }}</td>
              <td>{{ c.tipo }}</td>
              <td><input type="time" class="form-control form-control-sm" [(ngModel)]="c.horaApertura" /></td>
              <td><input type="time" class="form-control form-control-sm" [(ngModel)]="c.horaCierre" /></td>
              <td><input type="number" min="1" class="form-control form-control-sm" [(ngModel)]="c.precioHora" /></td>
              <td class="text-end"><button class="btn btn-primary btn-sm" (click)="guardar(c)">Guardar</button></td>
            </tr>
          }
        </tbody>
      </table>
      <p class="small text-muted mt-3 mb-0">Los cambios de horario se reflejan de inmediato en la disponibilidad pública. Las reservas existentes conservan su tarifa original.</p>
    </div>
  `,
})
export class HorariosAdmin {
  private readonly api = inject(ApiService);
  protected readonly canchas = signal<Cancha[]>([]);
  protected readonly mensaje = signal('');
  protected readonly ok = signal(true);

  constructor() { this.api.canchasAdmin().subscribe(c => this.canchas.set(c)); }

  protected guardar(c: Cancha) {
    this.api.horario(c.id, { horaApertura: c.horaApertura.slice(0, 5), horaCierre: c.horaCierre.slice(0, 5), precioHora: +c.precioHora }).subscribe({
      next: () => { this.mensaje.set(`Horario de ${c.nombre} actualizado.`); this.ok.set(true); },
      error: e => { this.mensaje.set(mensajeError(e)); this.ok.set(false); },
    });
  }
}
