import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { getApiErrorMessage } from '../../../../core/http/api-error';
import { AuthApi } from '../../services/auth-api';
import { AuthState } from '../../../../core/auth/auth-state';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';

@Component({
  selector: 'app-change-password-page',
  imports: [ReactiveFormsModule, RouterLink, AppAlert],
  templateUrl: './change-password-page.html',
  styleUrl: './change-password-page.scss'
})
export class ChangePasswordPage {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly authApi = inject(AuthApi);
  private readonly authState = inject(AuthState);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly form = this.formBuilder.group({
    currentPassword: ['', [Validators.required]],
    newPassword: ['', [Validators.required]]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    const { currentPassword, newPassword } = this.form.getRawValue();

    this.authApi.changePassword(currentPassword, newPassword).subscribe({
      next: response => {
        // Keep this session alive with the new token; every other session is now revoked.
        this.authState.setAccessToken(response.accessToken);
        this.successMessage.set(response.message);
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