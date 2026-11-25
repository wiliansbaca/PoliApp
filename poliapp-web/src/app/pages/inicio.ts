import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { AuthService } from '../core/auth.service';

@Component({
  selector: 'app-inicio',
  imports: [RouterLink],
  template: `
    <section class="text-white text-center py-5" style="background: linear-gradient(135deg,#0f5132,#198754)">
      <div class="container py-4">
        <h1 class="display-5 fw-bold">Reserva tu cancha sintética en Cusco</h1>
        <p class="lead mb-4">Consulta la disponibilidad en tiempo real y reserva en segundos, sin llamadas ni mensajes.</p>
        <a class="btn btn-light btn-lg me-2" routerLink="/canchas"><i class="bi bi-calendar-check me-1"></i>Ver disponibilidad</a>
        @if (!auth.autenticado()) {
          <a class="btn btn-outline-light btn-lg" routerLink="/login">Crear cuenta</a>
        }
      </div>
    </section>
    <div class="container py-5">
      <div class="row g-4 text-center">
        <div class="col-md-4"><div class="card h-100 p-4"><i class="bi bi-clock-history fs-1 text-success"></i><h5 class="mt-2">Disponibilidad en línea</h5><p class="text-muted mb-0">Horarios libres y ocupados de cada cancha, actualizados al instante.</p></div></div>
        <div class="col-md-4"><div class="card h-100 p-4"><i class="bi bi-shield-check fs-1 text-success"></i><h5 class="mt-2">Sin cruces de horario</h5><p class="text-muted mb-0">El sistema valida cada reserva para que nunca se duplique un turno.</p></div></div>
        <div class="col-md-4"><div class="card h-100 p-4"><i class="bi bi-phone fs-1 text-success"></i><h5 class="mt-2">Paga con Yape o Plin</h5><p class="text-muted mb-0">Registra tu pago y el administrador lo valida para confirmar tu turno.</p></div></div>
      </div>
    </div>
  `,
})
export class Inicio {
  protected readonly auth = inject(AuthService);
}
