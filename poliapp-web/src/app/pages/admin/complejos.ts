import { Component, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { Cancha, Complejo, mensajeError } from '../../core/models';

const VACIA: Partial<Cancha> = { nombre: '', tipo: 'Fútbol 7', precioHora: 70, horaApertura: '07:00', horaCierre: '23:00', activa: true };

/** HU02: gestión de complejos y canchas. */
@Component({
  selector: 'app-complejos-admin',
  imports: [FormsModule, CurrencyPipe],
  template: `
    <h3 class="mb-3">Complejos y canchas</h3>
    @if (mensaje()) { <div class="alert py-2" [class.alert-success]="ok()" [class.alert-danger]="!ok()">{{ mensaje() }}</div> }
    @for (c of complejos(); track c.id) {
      <div class="card p-4 mb-3">
        <h5>Datos del complejo</h5>
        <div class="row g-2">
          <div class="col-md-4"><label class="form-label small" for="cn">Nombre</label><input id="cn" class="form-control" [(ngModel)]="c.nombre" /></div>
          <div class="col-md-4"><label class="form-label small" for="cd">Dirección</label><input id="cd" class="form-control" [(ngModel)]="c.direccion" /></div>
          <div class="col-md-2"><label class="form-label small" for="cs">Distrito</label><input id="cs" class="form-control" [(ngModel)]="c.distrito" /></div>
          <div class="col-md-2"><label class="form-label small" for="ct">Teléfono</label><input id="ct" class="form-control" [(ngModel)]="c.telefono" /></div>
        </div>
        <div class="mt-3"><button class="btn btn-primary btn-sm" (click)="guardarComplejo(c)">Guardar complejo</button></div>
      </div>
    }

    <div class="card p-4">
      <div class="d-flex justify-content-between align-items-center mb-2">
        <h5 class="mb-0">Canchas</h5>
        <button class="btn btn-outline-primary btn-sm" (click)="nueva()"><i class="bi bi-plus-lg"></i> Nueva cancha</button>
      </div>
      <table class="table align-middle">
        <thead class="table-light"><tr><th>Cancha</th><th>Tipo</th><th>Tarifa</th><th>Horario</th><th>Estado</th><th></th></tr></thead>
        <tbody>
          @for (k of canchas(); track k.id) {
            <tr>
              <td>{{ k.nombre }}</td><td>{{ k.tipo }}</td><td>{{ k.precioHora | currency: 'PEN' : 'S/ ' }}</td>
              <td>{{ k.horaApertura }} – {{ k.horaCierre }}</td>
              <td><span class="badge" [class]="k.activa ? 'text-bg-success' : 'text-bg-secondary'">{{ k.activa ? 'Activa' : 'Inactiva' }}</span></td>
              <td class="text-end"><button class="btn btn-sm btn-outline-secondary" (click)="editar(k)"><i class="bi bi-pencil"></i> Editar</button></td>
            </tr>
          }
        </tbody>
      </table>

      @if (form(); as f) {
        <div class="border rounded p-3 bg-light">
          <h6>{{ f.id ? 'Editar ' + f.nombre : 'Nueva cancha' }}</h6>
          <div class="row g-2">
            <div class="col-md-3"><label class="form-label small" for="kn">Nombre</label><input id="kn" class="form-control" [(ngModel)]="f.nombre" /></div>
            <div class="col-md-3"><label class="form-label small" for="kt">Tipo</label>
              <select id="kt" class="form-select" [(ngModel)]="f.tipo">
                @for (t of tipos; track t) { <option>{{ t }}</option> }
              </select></div>
            <div class="col-md-2"><label class="form-label small" for="kp">Tarifa (S/)</label><input id="kp" type="number" class="form-control" [(ngModel)]="f.precioHora" /></div>
            <div class="col-md-2"><label class="form-label small" for="ka">Apertura</label><input id="ka" type="time" class="form-control" [(ngModel)]="f.horaApertura" /></div>
            <div class="col-md-2"><label class="form-label small" for="kc">Cierre</label><input id="kc" type="time" class="form-control" [(ngModel)]="f.horaCierre" /></div>
          </div>
          <div class="form-check mt-2"><input id="kv" class="form-check-input" type="checkbox" [(ngModel)]="f.activa" /><label class="form-check-label" for="kv">Activa</label></div>
          <div class="mt-2">
            <button class="btn btn-primary btn-sm me-2" (click)="guardarCancha(f)">Guardar</button>
            <button class="btn btn-secondary btn-sm" (click)="form.set(null)">Cancelar</button>
          </div>
        </div>
      }
    </div>
  `,
})
export class ComplejosAdmin {
  private readonly api = inject(ApiService);
  protected readonly tipos = ['Fútbol 5', 'Fútbol 6', 'Fútbol 7', 'Fútbol 8', 'Vóley', 'Básquet'];
  protected readonly complejos = signal<Complejo[]>([]);
  protected readonly canchas = signal<Cancha[]>([]);
  protected readonly form = signal<Partial<Cancha> | null>(null);
  protected readonly mensaje = signal('');
  protected readonly ok = signal(true);

  constructor() {
    this.api.complejos().subscribe(c => this.complejos.set(c));
    this.cargarCanchas();
  }

  private cargarCanchas() { this.api.canchasAdmin().subscribe(c => this.canchas.set(c)); }
  private aviso(t: string, ok: boolean) { this.mensaje.set(t); this.ok.set(ok); }

  protected guardarComplejo(c: Complejo) {
    this.api.editarComplejo(c.id, c).subscribe({ next: () => this.aviso('Complejo actualizado.', true), error: e => this.aviso(mensajeError(e), false) });
  }

  protected nueva() { this.form.set({ ...VACIA, complejoId: this.complejos()[0]?.id }); }
  protected editar(k: Cancha) { this.form.set({ ...k }); }

  protected guardarCancha(f: Partial<Cancha>) {
    const op = f.id ? this.api.editarCancha(f.id, f) : this.api.crearCancha(f);
    op.subscribe({
      next: () => { this.form.set(null); this.cargarCanchas(); this.aviso('Cancha guardada.', true); },
      error: e => this.aviso(mensajeError(e), false),
    });
  }
}
