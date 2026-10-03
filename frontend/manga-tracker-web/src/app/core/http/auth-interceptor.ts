import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';

import { environment } from '../../../environments/environment';
import { AuthState } from '../auth/auth-state';

// The token is only attached to requests for our own API, never to any other origin.
const isApiRequest = (url: string): boolean =>
  url.startsWith(`${environment.apiUrl}/api/`);

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authState = inject(AuthState);
  const router = inject(Router);

  const accessToken = authState.accessToken();

  const requestToSend = accessToken && isApiRequest(request.url)
    ? request.clone({
        setHeaders: {
          Authorization: `Bearer ${accessToken}`
        }
      })
    : request;

  return next(requestToSend).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse && error.status === 401) {
        authState.clearSession();
        router.navigateByUrl('/login');
      }

      return throwError(() => error);
    })
  );
};