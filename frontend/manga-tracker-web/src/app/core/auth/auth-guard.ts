import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';

import { AuthState } from './auth-state';

export const authGuard: CanActivateFn = (_route, state) => {
  const authState = inject(AuthState);
  const router = inject(Router);

  if (authState.isAuthenticated()) {
    return true;
  }

  return router.createUrlTree(['/login'], {
    queryParams: {
      returnUrl: state.url
    }
  });
};