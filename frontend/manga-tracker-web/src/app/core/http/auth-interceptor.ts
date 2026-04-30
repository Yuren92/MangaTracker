import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';

import { AuthState } from '../auth/auth-state';

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authState = inject(AuthState);
  const accessToken = authState.accessToken();

  if (!accessToken) {
    return next(request);
  }

  const authenticatedRequest = request.clone({
    setHeaders: {
      Authorization: `Bearer ${accessToken}`
    }
  });

  return next(authenticatedRequest);
};