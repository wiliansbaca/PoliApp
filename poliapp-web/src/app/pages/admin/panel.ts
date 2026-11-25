import { Component, inject } from '@angular/core';
import { CurrencyPipe } from '@angular/common';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { ApiService } from '../../core/api.service';
import { AuthService } from '../../core/auth.service';

@Component({
  selector: 'app-panel-admin',
  imports: [CurrencyPipe, RouterLink],
  template: `
    <h3 class="mb-1">Panel del administrador</h3>
    <p class="text-muted">Bienvenido, {{ auth.sesion()?.nombre }}.</p>
    @if (panel(); as p) {
      <div class="row g-3">
        <div class="col-sm-6 col-xl-3"><div class="card p-3 kpi"><span class="text-muted">Reservas de hoy</span><span class="valor">{{ p.reservasHoy }}</span></div></div>
        <div class="col-sm-6 col-xl-3"><a routerLink="/admin/reservas" class="text-decoration-none"><div class="card p-3 kpi"><span class="text-muted">Pendientes por atender</span><span class="valor text-warning">{{ p.pendientesPorAtender }}</span></div></a></div>
        <div class="col-sm-6 col-xl-3"><a routerLink="/admin/pagos" class="text-decoration-none"><div class="card p-3 kpi"><span class="text-muted">Pagos por validar</span><span class="valor text-primary">{{ p.pagosPorValidar }}</span></div></a></div>
        <div class="col-sm-6 col-xl-3"><div class="card p-3 kpi"><span class="text-muted">Ingresos del mes</span><span class="valor text-success">{{ p.ingresosMes | currency: 'PEN' : 'S/ ' }}</span></div></div>
      </div>
      <div class="card p-3 mt-3">
        <span class="text-muted">Ocupación de los últimos 7 días</span>
        <div class="progress mt-2" style="height: 22px">
          <div class="progress-bar bg-success" [style.width.%]="p.ocupacionSemana">{{ p.ocupacionSemana }}%</div>
        </div>
      </div>
    }
  `,
})
export class PanelAdmin {
  protected readonly auth = inject(AuthService);
  protected readonly panel = toSignal(inject(ApiService).panel());
}
