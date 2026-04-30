import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthState } from '../../../../core/auth/auth-state';

import { AuthApi } from '../../services/auth-api';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss'
})
export class LoginPage {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly authApi = inject(AuthApi);
  private readonly router = inject(Router);

  private readonly authState = inject(AuthState);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

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

    this.authApi.login(this.form.getRawValue()).subscribe({
      next: response => {
        this.authState.setAccessToken(response.accessToken);
        this.authState.loadCurrentUser();
        this.router.navigateByUrl('/collection');
      },
      error: error => {
        this.errorMessage.set(
          error?.error?.detail ?? 'No se ha podido iniciar sesión.'
        );

        this.isSubmitting.set(false);
      }
    });
  }
}