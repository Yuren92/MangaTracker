import { computed, inject, Injectable, signal } from '@angular/core';

import { AuthApi } from '../../features/auth/services/auth-api';
import { CurrentUserResponse } from '../../features/auth/models/auth.models';

const ACCESS_TOKEN_KEY = 'accessToken';

@Injectable({
  providedIn: 'root'
})
export class AuthState {
  private readonly authApi = inject(AuthApi);

  private readonly accessTokenSignal = signal<string | null>(
    localStorage.getItem(ACCESS_TOKEN_KEY)
  );

  private readonly currentUserSignal = signal<CurrentUserResponse | null>(null);

  readonly accessToken = this.accessTokenSignal.asReadonly();
  readonly currentUser = this.currentUserSignal.asReadonly();

  readonly isAuthenticated = computed(() => !!this.accessTokenSignal());

  setAccessToken(accessToken: string): void {
    localStorage.setItem(ACCESS_TOKEN_KEY, accessToken);
    this.accessTokenSignal.set(accessToken);
  }

  clearSession(): void {
    localStorage.removeItem(ACCESS_TOKEN_KEY);
    this.accessTokenSignal.set(null);
    this.currentUserSignal.set(null);
  }

  loadCurrentUser(): void {
    if (!this.accessTokenSignal()) {
      this.currentUserSignal.set(null);
      return;
    }

    this.authApi.getCurrentUser().subscribe({
      next: user => {
        this.currentUserSignal.set(user);
      },
      error: () => {
        this.clearSession();
      }
    });
  }
}