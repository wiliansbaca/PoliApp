import { Component, inject, input, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { AuthService } from '../core/auth.service';
import { mensajeError, Sesion } from '../core/models';

/** HU01: registro e inicio de sesión. */
@Component({
  selector: 'app-login',
  imports: [FormsModule],
  template: `
    <div class="container py-5">
      <div class="row g-4 justify-content-center">
        <div class="col-lg-5">
          <div class="card p-4 h-100">
            <h4 class="mb-3">Iniciar sesión</h4>
            <form #f="ngForm" (ngSubmit)="ingresar()">
              <div class="mb-3">
                <label class="form-label" for="lc">Correo electrónico</label>
                <input id="lc" class="form-control" name="correo" type="email" [(ngModel)]="correo" required />
              </div>
              <div class="mb-3">
                <label class="form-label" for="lp">Contraseña</label>
                <input id="lp" class="form-control" name="password" type="password" [(ngModel)]="password" required />
              </div>
              @if (errorLogin()) { <div class="alert alert-danger py-2"><i class="bi bi-exclamation-triangle me-1"></i>{{ errorLogin() }}</div> }
              <button class="btn btn-primary w-100" [disabled]="f.invalid || cargando()">Ingresar</button>
            </form>
          </div>
        </div>
        <div class="col-lg-5">
          <div class="card p-4 h-100">
            <h4 class="mb-3">Crear cuenta</h4>
            <form #r="ngForm" (ngSubmit)="registrar()">
              <div class="row g-2">
                <div class="col-6 mb-2"><label class="form-label" for="rn">Nombres</label><input id="rn" class="form-control" name="nombres" [(ngModel)]="reg.nombres" required /></div>
                <div class="col-6 mb-2"><label class="form-label" for="ra">Apellidos</label><input id="ra" class="form-control" name="apellidos" [(ngModel)]="reg.apellidos" required /></div>
              </div>
              <div class="mb-2"><label class="form-label" for="rc">Correo electrónico</label><input id="rc" class="form-control" type="email" name="correo" [(ngModel)]="reg.correo" required email /></div>
              <div class="mb-2"><label class="form-label" for="rt">Celular</label><input id="rt" class="form-control" name="celular" [(ngModel)]="reg.celular" required pattern="9[0-9]{8}" placeholder="9XXXXXXXX" /></div>
              <div class="mb-3"><label class="form-label" for="rp">Contraseña</label><input id="rp" class="form-control" type="password" name="password" [(ngModel)]="reg.password" required minlength="8" /></div>
              @if (errorRegistro()) { <div class="alert alert-danger py-2">{{ errorRegistro() }}</div> }
              <button class="btn btn-primary w-100" [disabled]="r.invalid || cargando()">Registrarme</button>
            </form>
          </div>
        </div>
      </div>
    </div>
  `,
})
export class Login {
  readonly volver = input<string>();
  private readonly auth = inject(AuthService);
  private readonly router = inject(Router);
  protected correo = '';
  protected password = '';
  protected reg = { nombres: '', apellidos: '', correo: '', celular: '', password: '' };
  protected readonly cargando = signal(false);
  protected readonly errorLogin = signal('');
  protected readonly errorRegistro = signal('');

  protected ingresar() {
    this.cargando.set(true);
    this.errorLogin.set('');
    this.auth.login(this.correo, this.password).subscribe({
      next: s => this.redirigir(s),
      error: e => { this.errorLogin.set(mensajeError(e)); this.cargando.set(false); },
    });
  }

  protected registrar() {
    this.cargando.set(true);
    this.errorRegistro.set('');
    this.auth.registrar(this.reg).subscribe({
      next: s => this.redirigir(s),
      error: e => { this.errorRegistro.set(mensajeError(e)); this.cargando.set(false); },
    });
  }

  private redirigir(s: Sesion) {
    this.router.navigateByUrl(this.volver() ?? (s.rol === 'Administrador' ? '/admin' : '/canchas'));
  }
}
