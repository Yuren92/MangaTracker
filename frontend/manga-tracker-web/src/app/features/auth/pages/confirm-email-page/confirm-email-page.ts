import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { AuthApi } from '../../services/auth-api';

@Component({
  selector: 'app-confirm-email-page',
  imports: [RouterLink],
  templateUrl: './confirm-email-page.html',
  styleUrl: './confirm-email-page.scss'
})
export class ConfirmEmailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly authApi = inject(AuthApi);

  readonly isLoading = signal(true);
  readonly successMessage = signal<string | null>(null);
  readonly errorMessage = signal<string | null>(null);

  constructor() {
    const token = this.route.snapshot.queryParamMap.get('token');

    if (!token) {
      this.isLoading.set(false);
      this.errorMessage.set('El enlace de confirmación no contiene token.');
      return;
    }

    this.authApi.confirmEmail(token).subscribe({
      next: response => {
        this.successMessage.set(response.message);
        this.isLoading.set(false);
      },
      error: error => {
        this.errorMessage.set(
          error?.error?.detail ?? 'No se ha podido confirmar el email.'
        );

        this.isLoading.set(false);
      }
    });
  }
}