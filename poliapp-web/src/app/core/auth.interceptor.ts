import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthService } from './auth.service';

/**
 * Antepone la URL de la API a las rutas /api (en producción la API está en otro dominio),
 * adjunta el token JWT a cada petición y cierra la sesión si el servidor responde 401.
 */
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const token = auth.sesion()?.token;
  const url = req.url.startsWith('/api') ? `${environment.apiUrl}${req.url}` : req.url;
  const peticion = req.clone({
    url,
    setHeaders: token ? { Authorization: `Bearer ${token}` } : {},
  });
  return next(peticion).pipe(
    catchError(err => {
      if (err.status === 401 && token) auth.salir();
      return throwError(() => err);
    }),
  );
};
