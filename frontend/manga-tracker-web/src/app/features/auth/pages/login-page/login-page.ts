import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';

import { AuthState } from '../../../../core/auth/auth-state';
import { EMAIL_NOT_CONFIRMED, getApiErrorDetail, getApiErrorMessage } from '../../../../core/http/api-error';
import { AppAlert } from '../../../../shared/components/app-alert/app-alert';
import { AuthApi } from '../../services/auth-api';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, RouterLink, AppAlert],
  templateUrl: './login-page.html',
  styleUrl: '../../auth-card.scss'
})
export class LoginPage {
  private readonly formBuilder = inject(NonNullableFormBuilder);
  private readonly authApi = inject(AuthApi);
  private readonly authState = inject(AuthState);
  private readonly router = inject(Router);
  private readonly route = inject(ActivatedRoute);

  readonly isSubmitting = signal(false);
  readonly errorMessage = signal<string | null>(null);
  readonly isEmailNotConfirmed = signal(false);

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
    this.isEmailNotConfirmed.set(false);

    this.authApi.login(this.form.getRawValue()).subscribe({
      next: response => {
        this.authState.setAccessToken(response.accessToken);
        this.authState.loadCurrentUser();

        this.router.navigateByUrl(this.safeReturnUrl());
      },
      error: error => {
        this.errorMessage.set(
          getApiErrorMessage(error, 'No se ha podido iniciar sesión.')
        );
        this.isEmailNotConfirmed.set(getApiErrorDetail(error) === EMAIL_NOT_CONFIRMED);

        this.isSubmitting.set(false);
      }
    });
  }

  // returnUrl comes from the address bar, so only app-relative paths are accepted;
  // anything else ("https://...", "//host") falls back to the collections page.
  private safeReturnUrl(): string {
    const returnUrl = this.route.snapshot.queryParamMap.get('returnUrl');

    return returnUrl && returnUrl.startsWith('/') && !returnUrl.startsWith('//')
      ? returnUrl
      : '/collections';
  }
}
