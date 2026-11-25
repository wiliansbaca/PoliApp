import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from './auth.service';

export const authGuard: CanActivateFn = (_, state) => {
  const auth = inject(AuthService);
  return auth.autenticado() || inject(Router).createUrlTree(['/login'], { queryParams: { volver: state.url } });
};

export const adminGuard: CanActivateFn = () => {
  const auth = inject(AuthService);
  return auth.esAdmin() || inject(Router).createUrlTree(['/login']);
};
