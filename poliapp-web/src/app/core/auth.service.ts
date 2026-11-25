import { HttpClient } from '@angular/common/http';
import { Injectable, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';
import { tap } from 'rxjs';
import { Sesion } from './models';

const CLAVE = 'poliapp.sesion';

/** HU01: guarda la sesión (token JWT) y expone el usuario actual. */
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly router = inject(Router);
  readonly sesion = signal<Sesion | null>(this.leer());
  readonly autenticado = computed(() => this.sesion() !== null);
  readonly esAdmin = computed(() => this.sesion()?.rol === 'Administrador');

  login(correo: string, password: string) {
    return this.http.post<Sesion>('/api/auth/login', { correo, password }).pipe(tap(s => this.guardar(s)));
  }

  registrar(datos: { nombres: string; apellidos: string; correo: string; celular: string; password: string }) {
    return this.http.post<Sesion>('/api/auth/registro', datos).pipe(tap(s => this.guardar(s)));
  }

  salir() {
    this.guardar(null);
    this.router.navigate(['/login']);
  }

  private guardar(s: Sesion | null) {
    this.sesion.set(s);
    try {
      if (s) localStorage.setItem(CLAVE, JSON.stringify(s));
      else localStorage.removeItem(CLAVE);
    } catch { /* almacenamiento no disponible */ }
  }

  private leer(): Sesion | null {
    try {
      const s = JSON.parse(localStorage.getItem(CLAVE) ?? 'null') as Sesion | null;
      return s && new Date(s.expira) > new Date() ? s : null;
    } catch {
      return null;
    }
  }
}
