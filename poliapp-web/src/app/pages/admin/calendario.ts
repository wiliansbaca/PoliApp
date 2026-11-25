import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { forkJoin, switchMap } from 'rxjs';
import { ApiService } from '../../core/api.service';
import { CalendarioCancha, mensajeError } from '../../core/models';

interface Fila extends CalendarioCancha { url: string; }

/**
 * HU07 y HU08: integración con Google Calendar sin Google Cloud, mediante el formato iCalendar.
 * 1) Las reservas de cada cancha se publican en un enlace .ics al que el administrador se suscribe.
 * 2) El administrador bloquea horarios en un calendario de Google y pega aquí su dirección secreta iCal.
 */
@Component({
  selector: 'app-calendario-admin',
  imports: [FormsModule],
  template: `
    <h3 class="mb-1">Google Calendar</h3>
    <p class="text-muted mb-3">Mira tus reservas en la app de Google Calendar y bloquea horarios desde ahí, sin entrar a Poli-App.</p>
    @if (mensaje()) { <div class="alert py-2" [class.alert-success]="ok()" [class.alert-danger]="!ok()">{{ mensaje() }}</div> }

    @for (c of filas(); track c.canchaId) {
      <div class="card p-4 mb-3">
        <h5 class="mb-3"><i class="bi bi-dribbble me-1"></i>{{ c.cancha }}</h5>

        <h6 class="fw-semibold">1. Ver las reservas en Google Calendar</h6>
        <p class="small text-muted mb-2">
          Agrega este enlace en Google Calendar (<i>Otros calendarios → + → Desde URL</i>) o usa el botón.
          Google lo actualiza cada pocas horas. No compartas el enlace: muestra los datos de tus clientes.
        </p>
        <div class="input-group input-group-sm mb-2">
          <input class="form-control font-monospace" [value]="c.urlSuscripcion" readonly aria-label="Enlace del calendario de reservas" />
          <button class="btn btn-outline-secondary" (click)="copiar(c.urlSuscripcion)"><i class="bi bi-clipboard"></i> Copiar</button>
          <a class="btn btn-success" [href]="suscribir(c.urlSuscripcion)" target="_blank" rel="noopener"><i class="bi bi-google"></i> Agregar a Google Calendar</a>
        </div>
        <button class="btn btn-link btn-sm px-0 mb-3" (click)="renovar(c)">Generar un enlace nuevo (el anterior deja de funcionar)</button>

        <h6 class="fw-semibold">2. Bloquear horarios desde Google Calendar</h6>
        <p class="small text-muted mb-2">
          Crea un calendario para esta cancha (por ejemplo «Bloqueos {{ c.cancha }}»). En su <i>Configuración → Integrar el calendario</i>,
          copia la <b>dirección secreta en formato iCal</b> y pégala aquí. Los eventos de ese calendario aparecerán como <b>Ocupado</b>
          en la disponibilidad (en un máximo de 5 minutos) y nadie podrá reservar en esos horarios.
        </p>
        <div class="input-group input-group-sm">
          <input class="form-control font-monospace" [(ngModel)]="c.url" placeholder="https://calendar.google.com/calendar/ical/.../private-.../basic.ics"
                 aria-label="Dirección secreta en formato iCal" />
          <button class="btn btn-primary" (click)="guardar(c)">Guardar</button>
          @if (c.icalOcupadosUrl) { <button class="btn btn-outline-danger" (click)="quitar(c)">Quitar</button> }
        </div>
        @if (c.icalOcupadosUrl) {
          <p class="small text-success mt-2 mb-0"><i class="bi bi-check-circle me-1"></i>Calendario de bloqueos vinculado.</p>
        }
      </div>
    } @empty {
      <p class="text-muted">Cargando canchas…</p>
    }
  `,
})
export class CalendarioAdmin {
  private readonly api = inject(ApiService);
  protected readonly filas = signal<Fila[]>([]);
  protected readonly mensaje = signal('');
  protected readonly ok = signal(true);

  constructor() {
    this.api.canchasAdmin()
      .pipe(switchMap(canchas => forkJoin(canchas.map(c => this.api.calendario(c.id)))))
      .subscribe({
        next: l => this.filas.set(l.map(c => ({ ...c, url: c.icalOcupadosUrl ?? '' }))),
        error: e => this.aviso(mensajeError(e), false),
      });
  }

  /** Abre Google Calendar con el formulario para suscribirse al enlace (protocolo webcal). */
  protected suscribir(url: string) {
    return 'https://calendar.google.com/calendar/render?cid=' + encodeURIComponent(url.replace(/^https?:\/\//, 'webcal://'));
  }

  protected copiar(texto: string) {
    navigator.clipboard?.writeText(texto).then(
      () => this.aviso('Enlace copiado.', true),
      () => this.aviso('No se pudo copiar; selecciona el enlace y cópialo manualmente.', false),
    );
  }

  protected guardar(c: Fila) { this.vincular(c, c.url.trim() || null); }

  protected quitar(c: Fila) {
    if (confirm(`¿Quitar el calendario de bloqueos de ${c.cancha}?`)) this.vincular(c, null);
  }

  protected renovar(c: Fila) {
    if (!confirm('El enlace actual dejará de funcionar y tendrás que volver a agregarlo en Google Calendar. ¿Continuar?')) return;
    this.api.renovarEnlaceCalendario(c.canchaId).subscribe({
      next: x => { this.reemplazar(x); this.aviso('Se generó un enlace nuevo.', true); },
      error: e => this.aviso(mensajeError(e), false),
    });
  }

  private vincular(c: Fila, url: string | null) {
    this.api.vincularCalendario(c.canchaId, url).subscribe({
      next: x => { this.reemplazar(x); this.aviso(url ? `Calendario de bloqueos vinculado a ${x.cancha}.` : `Se quitó el calendario de ${x.cancha}.`, true); },
      error: e => this.aviso(mensajeError(e), false),
    });
  }

  private reemplazar(x: CalendarioCancha) {
    this.filas.update(l => l.map(f => (f.canchaId === x.canchaId ? { ...x, url: x.icalOcupadosUrl ?? '' } : f)));
  }

  private aviso(texto: string, ok: boolean) { this.mensaje.set(texto); this.ok.set(ok); }
}
