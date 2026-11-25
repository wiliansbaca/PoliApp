import { Routes } from '@angular/router';
import { adminGuard, authGuard } from './core/guards';

export const routes: Routes = [
  { path: '', loadComponent: () => import('./pages/inicio').then(m => m.Inicio), title: 'Poli-App' },
  { path: 'login', loadComponent: () => import('./pages/login').then(m => m.Login), title: 'Ingresar | Poli-App' },
  { path: 'canchas', loadComponent: () => import('./pages/disponibilidad').then(m => m.Disponibilidad), title: 'Canchas | Poli-App' },
  { path: 'reservar', canActivate: [authGuard], loadComponent: () => import('./pages/reservar').then(m => m.Reservar), title: 'Reservar | Poli-App' },
  { path: 'mis-reservas', canActivate: [authGuard], loadComponent: () => import('./pages/mis-reservas').then(m => m.MisReservas), title: 'Mis reservas | Poli-App' },
  {
    path: 'admin',
    canActivate: [adminGuard],
    loadComponent: () => import('./pages/admin/admin-layout').then(m => m.AdminLayout),
    children: [
      { path: '', loadComponent: () => import('./pages/admin/panel').then(m => m.PanelAdmin), title: 'Panel | Poli-App' },
      { path: 'complejos', loadComponent: () => import('./pages/admin/complejos').then(m => m.ComplejosAdmin), title: 'Complejos y canchas | Poli-App' },
      { path: 'horarios', loadComponent: () => import('./pages/admin/horarios').then(m => m.HorariosAdmin), title: 'Horarios y tarifas | Poli-App' },
      { path: 'calendario', loadComponent: () => import('./pages/admin/calendario').then(m => m.CalendarioAdmin), title: 'Google Calendar | Poli-App' },
      { path: 'reservas', loadComponent: () => import('./pages/admin/reservas').then(m => m.ReservasAdmin), title: 'Reservas | Poli-App' },
      { path: 'reportes', loadComponent: () => import('./pages/admin/reportes').then(m => m.ReportesAdmin), title: 'Reportes | Poli-App' },
      { path: 'pagos', loadComponent: () => import('./pages/admin/pagos').then(m => m.PagosAdmin), title: 'Pagos | Poli-App' },
    ],
  },
  { path: '**', redirectTo: '' },
];
