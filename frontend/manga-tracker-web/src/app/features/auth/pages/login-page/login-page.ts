import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { AuthState } from '../../../../core/auth/auth-state';
import { getApiErrorMessage } from '../../../../core/http/api-error';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';

import { AuthApi } from '../../services/auth-api';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, RouterLink, AppAlert],
  templateUrl: './login-page.html',
  styleUrl: './login-page.scss'
})
export class LoginPage {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly authApi = inject(AuthApi);
  private readonly router = inject(Router);

  private readonly authState = inject(AuthState);
  private readonly route = inject(ActivatedRoute);

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

        const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');

        const safeReturnUrl =
          returnUrl && returnUrl.startsWith('/')
            ? returnUrl
            : '/collections';

        this.router.navigateByUrl(safeReturnUrl);
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido iniciar sesión.')
        );

        this.isSubmitting.set(false);
      }
    });
  }
}