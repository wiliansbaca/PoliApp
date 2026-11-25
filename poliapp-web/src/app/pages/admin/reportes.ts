import { Component, computed, inject, signal } from '@angular/core';
import { CurrencyPipe, DatePipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ApiService } from '../../core/api.service';
import { fechaLocal, mensajeError, Reporte } from '../../core/models';

/** HU09: reportes de ocupación e ingresos. */
@Component({
  selector: 'app-reportes-admin',
  imports: [FormsModule, CurrencyPipe, DatePipe],
  template: `
    <h3 class="mb-3">Reportes de ocupación e ingresos</h3>
    <div class="card p-3 mb-3">
      <div class="row g-2 align-items-end">
        <div class="col-md-3"><label class="form-label small" for="de">Desde</label><input id="de" type="date" class="form-control" [(ngModel)]="desde" /></div>
        <div class="col-md-3"><label class="form-label small" for="ha">Hasta</label><input id="ha" type="date" class="form-control" [(ngModel)]="hasta" /></div>
        <div class="col-md-3"><button class="btn btn-primary" (click)="generar()"><i class="bi bi-bar-chart me-1"></i>Generar</button></div>
      </div>
    </div>
    @if (error()) { <div class="alert alert-danger">{{ error() }}</div> }
    @if (rep(); as r) {
      <div class="row g-3 mb-3">
        <div class="col-md-4"><div class="card p-3 kpi"><span class="text-muted">Reservas atendidas</span><span class="valor">{{ r.totalReservas }}</span></div></div>
        <div class="col-md-4"><div class="card p-3 kpi"><span class="text-muted">Ingresos</span><span class="valor text-success">{{ r.totalIngresos | currency: 'PEN' : 'S/ ' }}</span></div></div>
        <div class="col-md-4"><div class="card p-3 kpi"><span class="text-muted">Ocupación promedio</span><span class="valor">{{ r.ocupacionPromedio }}%</span></div></div>
      </div>
      <div class="row g-3">
        <div class="col-12">
          <div class="card p-3 h-100">
            <h6>Ingresos por día (S/)</h6>
            <div class="d-flex align-items-end gap-1 mt-2" style="height: 200px">
              @for (d of r.porDia; track d.fecha) {
                <div class="flex-fill d-flex flex-column align-items-center justify-content-end h-100" [title]="d.fecha + ': S/ ' + d.ingresos">
                  <small class="text-muted" style="font-size:.65rem">{{ d.ingresos || '' }}</small>
                  <div class="barra w-100" [style.height.%]="maximo() ? d.ingresos / maximo() * 85 : 0"></div>
                  <small class="text-muted" style="font-size:.65rem">{{ d.fecha + 'T00:00' | date: 'dd/MM' }}</small>
                </div>
              }
            </div>
          </div>
        </div>
        <div class="col-12">
          <div class="card p-3 h-100">
            <h6>Por cancha</h6>
            <table class="table table-sm align-middle mb-0">
              <thead class="table-light"><tr><th>Cancha</th><th>Reservas</th><th>Ocupación</th><th class="text-end">Ingresos</th></tr></thead>
              <tbody>
                @for (c of r.porCancha; track c.canchaId) {
                  <tr>
                    <td>{{ c.cancha }}</td><td>{{ c.reservas }}</td>
                    <td style="width:45%"><div class="progress" style="height:16px"><div class="progress-bar bg-success" [style.width.%]="c.ocupacion">{{ c.ocupacion }}%</div></div></td>
                    <td class="text-end">{{ c.ingresos | currency: 'PEN' : 'S/ ' }}</td>
                  </tr>
                }
              </tbody>
            </table>
          </div>
        </div>
      </div>
    }
  `,
})
export class ReportesAdmin {
  private readonly api = inject(ApiService);
  protected hasta = fechaLocal();
  protected desde = fechaLocal(new Date(Date.now() - 13 * 86400000));
  protected readonly rep = signal<Reporte | null>(null);
  protected readonly error = signal('');
  protected readonly maximo = computed(() => Math.max(0, ...(this.rep()?.porDia.map(d => d.ingresos) ?? [])));

  constructor() { this.generar(); }

  protected generar() {
    this.error.set('');
    this.api.reporte(this.desde, this.hasta).subscribe({ next: r => this.rep.set(r), error: e => this.error.set(mensajeError(e)) });
  }
}
