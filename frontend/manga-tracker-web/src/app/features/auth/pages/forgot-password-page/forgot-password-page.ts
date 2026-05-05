import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { getApiErrorMessage } from '../../../../core/http/api-error';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';
import { AuthApi } from '../../services/auth-api';

@Component({
  selector: 'app-forgot-password-page',
  imports: [ReactiveFormsModule, RouterLink, AppAlert],
  templateUrl: './forgot-password-page.html',
  styleUrl: './forgot-password-page.scss'
})
export class ForgotPasswordPage {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly authApi = inject(AuthApi);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly form = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email]]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.authApi.forgotPassword(this.form.controls.email.value).subscribe({
      next: response => {
        this.successMessage.set(response.message);
        this.isSubmitting.set(false);
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido solicitar la recuperación.')
        );

        this.isSubmitting.set(false);
      }
    });
  }
}