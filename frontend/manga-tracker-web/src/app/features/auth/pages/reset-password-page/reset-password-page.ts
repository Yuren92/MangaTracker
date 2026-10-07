import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { getApiErrorMessage } from '../../../../core/http/api-error';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';
import { PASSWORD_RULES_HINT, strongPasswordValidator } from '../../../../shared/validators/password-validator';
import { AuthApi } from '../../services/auth-api';

@Component({
  selector: 'app-reset-password-page',
  imports: [ReactiveFormsModule, RouterLink, AppAlert],
  templateUrl: './reset-password-page.html',
  styleUrl: '../../auth-card.scss'
})
export class ResetPasswordPage {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly route = inject(ActivatedRoute);
  private readonly authApi = inject(AuthApi);

  readonly passwordRulesHint = PASSWORD_RULES_HINT;

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  private readonly token = this.route.snapshot.queryParamMap.get('token');

  readonly form = this.formBuilder.group({
    newPassword: ['', [Validators.required, strongPasswordValidator]]
  });

  submit(): void {
    if (!this.token) {
      this.errorMessage.set('El enlace de recuperación no contiene token.');
      return;
    }

    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.authApi.resetPassword(
      this.token,
      this.form.controls.newPassword.value
    ).subscribe({
      next: () => {
        this.successMessage.set('Contraseña cambiada. Ya puedes iniciar sesión con la nueva.');
        this.form.reset();
        this.isSubmitting.set(false);
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido cambiar la contraseña.')
        );

        this.isSubmitting.set(false);
      }
    });
  }
}
