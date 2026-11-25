import { Component } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';

@Component({
  selector: 'app-admin-layout',
  imports: [RouterOutlet, RouterLink, RouterLinkActive],
  template: `
    <div class="d-flex flex-column flex-md-row" style="min-height: calc(100vh - 56px)">
      <aside class="bg-dark text-white p-0" style="min-width: 220px">
        <nav class="nav flex-md-column py-2">
          @for (m of menu; track m.ruta) {
            <a class="nav-link text-white-50 px-3 py-2" [routerLink]="m.ruta" routerLinkActive="active bg-success text-white"
               [routerLinkActiveOptions]="{ exact: m.ruta === '/admin' }"><i class="bi me-2" [class]="m.icono"></i>{{ m.texto }}</a>
          }
        </nav>
      </aside>
      <main class="flex-grow-1 p-4"><router-outlet /></main>
    </div>
  `,
})
export class AdminLayout {
  protected readonly menu = [
    { ruta: '/admin', texto: 'Panel', icono: 'bi-speedometer2' },
    { ruta: '/admin/complejos', texto: 'Complejos y canchas', icono: 'bi-building' },
    { ruta: '/admin/horarios', texto: 'Horarios y tarifas', icono: 'bi-clock' },
    { ruta: '/admin/reservas', texto: 'Reservas', icono: 'bi-calendar-check' },
    { ruta: '/admin/calendario', texto: 'Google Calendar', icono: 'bi-google' },
    { ruta: '/admin/reportes', texto: 'Reportes', icono: 'bi-bar-chart' },
    { ruta: '/admin/pagos', texto: 'Pagos', icono: 'bi-cash-coin' },
  ];
}
