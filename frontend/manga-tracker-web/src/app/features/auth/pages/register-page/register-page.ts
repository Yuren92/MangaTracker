import { Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { getApiErrorMessage } from '../../../../core/http/api-error';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';
import { AuthApi } from '../../services/auth-api';

@Component({
  selector: 'app-register-page',
  imports: [ReactiveFormsModule, RouterLink, AppAlert],
  templateUrl: './register-page.html',
  styleUrl: './register-page.scss'
})
export class RegisterPage {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly authApi = inject(AuthApi);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly successMessage = signal<string | null>(null);

  readonly form = this.formBuilder.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.isSubmitting.set(true);
    this.errorMessage.set(null);
    this.successMessage.set(null);

    this.authApi.register(this.form.getRawValue()).subscribe({
      next: () => {
        // The API answers the same whether or not the email already had an account.
        this.successMessage.set(
          'Si el email se puede usar, te hemos enviado un correo con los siguientes pasos.'
        );

        this.form.reset();
        this.isSubmitting.set(false);
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido crear la cuenta.')
        );

        this.isSubmitting.set(false);
      }
    });
  }
}