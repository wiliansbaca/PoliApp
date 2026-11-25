import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { AuthService } from './auth.service';

/** Adjunta el token JWT a cada petición y cierra la sesión si el servidor responde 401. */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.sesion()?.token;
  const peticion = token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
  return next(peticion).pipe(
    catchError(err => {
      if (err.status === 401 && token) auth.salir();
      return throwError(() => err);
    }),
  );
};
