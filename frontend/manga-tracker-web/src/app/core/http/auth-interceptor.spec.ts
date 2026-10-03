import { TestBed } from '@angular/core/testing';
import { HttpClient, provideHttpClient, withInterceptors } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { Router } from '@angular/router';

import { environment } from '../../../environments/environment';
import { AuthState } from '../auth/auth-state';
import { authInterceptor } from './auth-interceptor';

describe('authInterceptor', () => {
  const apiUrl = `${environment.apiUrl}/api/collections`;

  let http: HttpClient;
  let httpTesting: HttpTestingController;
  let accessToken: string | null;
  let clearSession: ReturnType<typeof vi.fn>;
  let navigateByUrl: ReturnType<typeof vi.fn>;

  beforeEach(() => {
    accessToken = 'test-token';
    clearSession = vi.fn();
    navigateByUrl = vi.fn();

    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        { provide: AuthState, useValue: { accessToken: () => accessToken, clearSession } },
        { provide: Router, useValue: { navigateByUrl } }
      ]
    });

    http = TestBed.inject(HttpClient);
    httpTesting = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpTesting.verify());

  it('adds the bearer token to requests for our API', () => {
    http.get(apiUrl).subscribe();

    const request = httpTesting.expectOne(apiUrl);
    expect(request.request.headers.get('Authorization')).toBe('Bearer test-token');
    request.flush({});
  });

  it('never sends the token to other origins', () => {
    const otherOrigin = 'https://comicvine.gamespot.com/api/volume/4050-1/';
    http.get(otherOrigin).subscribe();

    const request = httpTesting.expectOne(otherOrigin);
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('does not add a header when there is no session', () => {
    accessToken = null;
    http.get(apiUrl).subscribe();

    const request = httpTesting.expectOne(apiUrl);
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({});
  });

  it('clears the session and goes to login on 401', () => {
    http.get(apiUrl).subscribe({ error: () => {} });

    httpTesting.expectOne(apiUrl).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(clearSession).toHaveBeenCalledOnce();
    expect(navigateByUrl).toHaveBeenCalledWith('/login');
  });
});
