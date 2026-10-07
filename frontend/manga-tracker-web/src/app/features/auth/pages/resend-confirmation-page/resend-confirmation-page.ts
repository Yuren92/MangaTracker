import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { getApiErrorMessage } from '../../../../core/http/api-error';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';
import { AuthApi } from '../../services/auth-api';

@Component({
  selector: 'app-resend-confirmation-page',
  imports: [ReactiveFormsModule, RouterLink, AppAlert],
  templateUrl: './resend-confirmation-page.html',
  styleUrl: '../../auth-card.scss'
})
export class ResendConfirmationPage {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly authApi = inject(AuthApi);
  private readonly route = inject(ActivatedRoute);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  // Pre-filled when coming from a login attempt with an unconfirmed email.
  readonly form = this.formBuilder.group({
    email: [
      this.route.snapshot.queryParamMap.get('email') ?? '',
      [Validators.required, Validators.email]
    ]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.authApi.resendConfirmationEmail(this.form.controls.email.value).subscribe({
      next: () => {
        // Same answer for every email, so the page cannot be used to find accounts.
        this.successMessage.set(
          'Si el email tiene una cuenta pendiente de confirmar, te hemos enviado un enlace nuevo.'
        );
        this.isSubmitting.set(false);
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido enviar el enlace de confirmación.')
        );

        this.isSubmitting.set(false);
      }
    });
  }
}
