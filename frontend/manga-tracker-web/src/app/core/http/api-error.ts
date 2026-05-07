import { HttpErrorResponse } from '@angular/common/http';

interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
}

export function getApiErrorMessage(
  error: unknown,
  fallbackMessage: string
): string {
  if (error instanceof HttpErrorResponse) {
    const problemDetails = error.error as ProblemDetails | null;

    if (problemDetails?.detail) {
      return problemDetails.detail;
    }

    if (problemDetails?.title) {
      return problemDetails.title;
    }

    if (error.status === 429) {
      return 'Has hecho demasiadas peticiones. Espera unos segundos y vuelve a intentarlo.';
    }

    if (error.status === 0) {
      return 'No se ha podido conectar con el servidor.';
    }
  }

  return fallbackMessage;
}