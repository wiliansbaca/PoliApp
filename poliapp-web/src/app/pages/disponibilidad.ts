import { Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { catchError, of, switchMap, tap } from 'rxjs';
import { ApiService } from '../core/api.service';
import { Disponibilidad as Disp, fechaLocal, mensajeError } from '../core/models';

/** HU04 y HU10: consulta de disponibilidad (también en vista móvil). */
@Component({
  selector: 'app-disponibilidad',
  imports: [FormsModule, CurrencyPipe],
  template: `
    <div class="container py-4">
      <h3 class="mb-3">Disponibilidad de canchas</h3>
      <div class="card p-3 mb-3">
        <div class="row g-2">
          <div class="col-md-4"><label class="form-label small" for="f">Fecha</label>
            <input id="f" type="date" class="form-control" [min]="hoy" [ngModel]="fecha()" (ngModelChange)="fecha.set($event)" /></div>
          <div class="col-md-4"><label class="form-label small" for="t">Tipo</label>
            <select id="t" class="form-select" [ngModel]="tipo()" (ngModelChange)="tipo.set($event)">
              <option value="">Todos</option>
              @for (t of filtros()?.tipos ?? []; track t) { <option [value]="t">{{ t }}</option> }
            </select></div>
          <div class="col-md-4"><label class="form-label small" for="d">Distrito</label>
            <select id="d" class="form-select" [ngModel]="distrito()" (ngModelChange)="distrito.set($event)">
              <option value="">Todos</option>
              @for (d of filtros()?.distritos ?? []; track d) { <option [value]="d">{{ d }}</option> }
            </select></div>
        </div>
      </div>

      @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }
      @for (d of datos(); track d.cancha.id) {
        <div class="card p-3 mb-3">
          <div class="d-flex flex-wrap justify-content-between align-items-baseline mb-2">
            <h6 class="mb-1"><b>{{ d.cancha.complejoNombre }}</b> · {{ d.cancha.nombre }}
              <span class="badge text-bg-light border ms-1">{{ d.cancha.tipo }}</span></h6>
            <span class="text-muted small">{{ d.cancha.distrito }} · {{ d.cancha.precioHora | currency: 'PEN' : 'S/ ' }} por hora</span>
          </div>
          <div class="d-flex flex-wrap gap-2">
            @for (b of d.bloques; track b.hora) {
              <button class="btn btn-sm slot" [class.libre]="b.libre" [class.ocupado]="!b.libre" [disabled]="!b.libre"
                      (click)="elegir(d.cancha.id, b.hora)">{{ b.hora }}</button>
            }
          </div>
        </div>
      } @empty {
        @if (!cargando()) { <p class="text-muted">No hay canchas para los filtros elegidos.</p> }
      }
      <p class="small text-muted"><span class="badge slot libre">Libre</span> <span class="badge slot ocupado">Ocupado</span> Toca un horario libre para reservarlo.</p>
    </div>
  `,
})
export class Disponibilidad {
  private readonly api = inject(ApiService);
  private readonly router = inject(Router);
  protected readonly hoy = fechaLocal();
  protected readonly fecha = signal(this.hoy);
  protected readonly tipo = signal('');
  protected readonly distrito = signal('');
  protected readonly cargando = signal(true);
  protected readonly error = signal('');
  protected readonly filtros = toSignal(this.api.filtros().pipe(catchError(() => of(null))));

  private readonly consulta = computed(() => ({ f: this.fecha(), t: this.tipo(), d: this.distrito() }));
  protected readonly datos = toSignal(
    toObservable(this.consulta).pipe(
      tap(() => { this.cargando.set(true); this.error.set(''); }),
      switchMap(q => this.api.disponibilidad(q.f, q.t, q.d).pipe(
        catchError(e => { this.error.set(mensajeError(e)); return of([] as Disp[]); }))),
      tap(() => this.cargando.set(false)),
    ),
    { initialValue: [] as Disp[] },
  );

  protected elegir(canchaId: number, hora: string) {
    this.router.navigate(['/reservar'], { queryParams: { cancha: canchaId, fecha: this.fecha(), hora } });
  }
}
