import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { getApiErrorMessage } from '../../../../core/http/api-error';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';
import { AuthApi } from '../../services/auth-api';

@Component({
  selector: 'app-confirm-email-page',
  imports: [RouterLink, AppAlert],
  templateUrl: './confirm-email-page.html',
  styleUrl: '../../auth-card.scss'
})
export class ConfirmEmailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly authApi = inject(AuthApi);

  readonly isLoading = signal(true);
  readonly isConfirmed = signal(false);
  readonly errorMessage = signal<string | null>(null);

  constructor() {
    const token = this.route.snapshot.queryParamMap.get('token');

    if (!token) {
      this.isLoading.set(false);
      this.errorMessage.set('El enlace de confirmación no contiene token.');
      return;
    }

    this.authApi.confirmEmail(token).subscribe({
      next: () => {
        this.isConfirmed.set(true);
        this.isLoading.set(false);
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido confirmar el email.')
        );

        this.isLoading.set(false);
      }
    });
  }
}
