import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ApiService } from '../core/api.service';
import { Bloque, Cancha, fechaLocal, mensajeError, sumarHora } from '../core/models';

/** HU05: formulario de reserva con validación de cruce de horario. */
@Component({
  selector: 'app-reservar',
  imports: [FormsModule, CurrencyPipe],
  template: `
    <div class="container py-4">
      <h3 class="mb-3">Registrar reserva</h3>
      @if (canchaSel(); as c) {
        <div class="row g-3">
          <div class="col-lg-8">
            <form class="card p-4" (ngSubmit)="confirmar()">
              <div class="mb-3"><label class="form-label" for="cx">Complejo</label>
                <input id="cx" class="form-control" [value]="c.complejoNombre" readonly /></div>
              <div class="mb-3"><label class="form-label" for="ca">Cancha</label>
                <input id="ca" class="form-control" [value]="c.nombre + ' – ' + c.tipo" readonly /></div>
              <div class="row g-2">
                <div class="col-md-4 mb-3"><label class="form-label" for="fe">Fecha</label>
                  <input id="fe" type="date" class="form-control" name="fecha" [min]="hoy" [ngModel]="fechaSel()" (ngModelChange)="fechaSel.set($event)" /></div>
                <div class="col-md-4 mb-3"><label class="form-label" for="hi">Hora de inicio</label>
                  <select id="hi" class="form-select" name="hi" [ngModel]="horaSel()" (ngModelChange)="horaSel.set($event)">
                    @for (b of bloques(); track b.hora) {
                      <option [value]="b.hora">{{ b.hora }}{{ b.libre ? '' : ' (ocupado)' }}</option>
                    }
                  </select></div>
                <div class="col-md-4 mb-3"><label class="form-label" for="du">Duración</label>
                  <select id="du" class="form-select" name="du" [ngModel]="horas()" (ngModelChange)="horas.set(+$event)">
                    <option [value]="1">1 hora</option><option [value]="2">2 horas</option><option [value]="3">3 horas</option>
                  </select></div>
              </div>
              @if (error()) { <div class="alert alert-danger py-2"><i class="bi bi-exclamation-triangle-fill me-1"></i>{{ error() }}</div> }
              @if (exito()) { <div class="alert alert-success py-2"><i class="bi bi-check-circle-fill me-1"></i>{{ exito() }}</div> }
              <div>
                <button class="btn btn-primary me-2" [disabled]="enviando()">Confirmar reserva</button>
                <button type="button" class="btn btn-secondary" (click)="volver()">Volver</button>
              </div>
            </form>
          </div>
          <div class="col-lg-4">
            <div class="card p-4">
              <h5>Resumen</h5>
              <p class="mb-1">{{ c.nombre }} · {{ c.tipo }}</p>
              <p class="mb-1">{{ fechaSel() }} · {{ horaSel() }} – {{ horaFin() }}</p>
              <p class="mb-3">{{ horas() }} hora(s) × {{ c.precioHora | currency: 'PEN' : 'S/ ' }}</p>
              <hr />
              <p class="fs-5">Total: <b>{{ total() | currency: 'PEN' : 'S/ ' }}</b></p>
              <p class="small text-muted mb-0">La reserva quedará <b>Pendiente</b> hasta que el administrador la confirme o se valide tu pago con Yape/Plin.</p>
            </div>
          </div>
        </div>
      } @else {
        <p class="text-muted">Cargando cancha…</p>
      }
    </div>
  `,
})
export class Reservar {
  readonly cancha = input<string>();
  readonly fecha = input<string>();
  readonly hora = input<string>();

  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  protected readonly hoy = fechaLocal();
  protected readonly canchaSel = signal<Cancha | null>(null);
  protected readonly bloques = signal<Bloque[]>([]);
  protected readonly fechaSel = signal(this.hoy);
  protected readonly horaSel = signal('18:00');
  protected readonly horas = signal(1);
  protected readonly enviando = signal(false);
  protected readonly error = signal('');
  protected readonly exito = signal('');

  protected readonly horaFin = computed(() => sumarHora(this.horaSel(), this.horas()));
  protected readonly total = computed(() => (this.canchaSel()?.precioHora ?? 0) * this.horas());

  constructor() {
    effect(() => {
      const id = +(this.cancha() ?? 0);
      if (this.fecha()) this.fechaSel.set(this.fecha()!);
      if (this.hora()) this.horaSel.set(this.hora()!);
      if (id) this.api.cancha(id).subscribe(c => this.canchaSel.set(c));
    });
    effect(() => {
      const c = this.canchaSel();
      const f = this.fechaSel();
      if (c) this.api.disponibilidad(f, undefined, undefined, c.id).subscribe(d => this.bloques.set(d[0]?.bloques ?? []));
    });
  }

  protected confirmar() {
    const c = this.canchaSel();
    if (!c) return;
    this.enviando.set(true);
    this.error.set('');
    this.api.reservar({ canchaId: c.id, fecha: this.fechaSel(), horaInicio: this.horaSel(), horaFin: this.horaFin() }).subscribe({
      next: r => {
        this.exito.set(`Reserva ${r.codigo} registrada. Te llevamos a tus reservas…`);
        setTimeout(() => this.router.navigate(['/mis-reservas']), 1300);
      },
      error: e => { this.error.set(mensajeError(e)); this.enviando.set(false); },
    });
  }

  protected volver() { this.router.navigate(['/canchas']); }
}
