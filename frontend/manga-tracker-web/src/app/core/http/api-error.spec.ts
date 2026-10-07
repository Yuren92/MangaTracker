import { HttpErrorResponse } from '@angular/common/http';

import { getApiErrorMessage } from './api-error';

describe('getApiErrorMessage', () => {
  const problem = (status: number, detail?: string) =>
    new HttpErrorResponse({ status, error: detail ? { status, detail } : null });

  it('translates errors the user can act on', () => {
    expect(getApiErrorMessage(problem(400, 'Invalid email or password.'), 'fallback'))
      .toBe('Email o contraseña incorrectos.');
  });

  it('never shows an untranslated API detail', () => {
    expect(getApiErrorMessage(problem(400, 'User id is required.'), 'Mensaje de la pantalla'))
      .toBe('Mensaje de la pantalla');
  });

  it('explains rate limiting and connection errors', () => {
    expect(getApiErrorMessage(problem(429), 'fallback')).toContain('demasiadas peticiones');
    expect(getApiErrorMessage(problem(0), 'fallback')).toContain('conectar con el servidor');
  });

  it('uses the fallback for anything that is not an HTTP error', () => {
    expect(getApiErrorMessage(new Error('boom'), 'fallback')).toBe('fallback');
  });
});
