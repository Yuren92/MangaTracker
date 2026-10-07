import { HttpErrorResponse } from '@angular/common/http';

interface ProblemDetails {
  title?: string;
  status?: number;
  detail?: string;
}

// The API answers in English: it is a contract for developers, while the copy the user
// reads belongs to the UI. Errors the user can act on are translated here; any other
// detail is replaced by the screen's own message, so English text never reaches the page.
const translatedDetails: Record<string, string> = {
  'Invalid email or password.': 'Email o contraseña incorrectos.',
  'Email is not confirmed.': 'Todavía no has confirmado tu email. Revisa tu correo o pide otro enlace.',
  'Email format is invalid.': 'El email no tiene un formato válido.',
  'Password must have at least 8 characters.': 'La contraseña debe tener al menos 8 caracteres.',
  'Password must contain at least one uppercase letter.': 'La contraseña debe tener al menos una mayúscula.',
  'Password must contain at least one lowercase letter.': 'La contraseña debe tener al menos una minúscula.',
  'Password must contain at least one number.': 'La contraseña debe tener al menos un número.',
  'Current password is invalid.': 'La contraseña actual no es correcta.',
  'New password must be different from current password.': 'La nueva contraseña debe ser distinta de la actual.',
  'Confirmation token is invalid or expired.': 'El enlace de confirmación no es válido o ha caducado.',
  'Password reset token is invalid or expired.': 'El enlace para restablecer la contraseña no es válido o ha caducado.',
  'Comic Vine is not available right now. Please try again later.':
    'Comic Vine no está disponible ahora mismo. Inténtalo de nuevo en unos minutos.',
  'Comic Vine volume was not found.': 'Esta edición ya no existe en Comic Vine.',
  'Collection was not found.': 'No se ha encontrado la colección.',
  'This volume has too many issues to import at once. Maximum allowed is 250.':
    'Esta edición tiene demasiados tomos para importarla de una vez (máximo 250).',
  'The data was modified by another request. Please try again.':
    'Otra petición ha modificado estos datos a la vez. Vuelve a intentarlo.'
};

export const EMAIL_NOT_CONFIRMED = 'Email is not confirmed.';

export function getApiErrorDetail(error: unknown): string | null {
  if (error instanceof HttpErrorResponse) {
    return (error.error as ProblemDetails | null)?.detail ?? null;
  }

  return null;
}

export function getApiErrorMessage(
  error: unknown,
  fallbackMessage: string
): string {
  if (!(error instanceof HttpErrorResponse)) {
    return fallbackMessage;
  }

  const detail = getApiErrorDetail(error);

  if (detail && translatedDetails[detail]) {
    return translatedDetails[detail];
  }

  if (error.status === 429) {
    return 'Has hecho demasiadas peticiones. Espera unos segundos y vuelve a intentarlo.';
  }

  if (error.status === 0) {
    return 'No se ha podido conectar con el servidor.';
  }

  return fallbackMessage;
}
