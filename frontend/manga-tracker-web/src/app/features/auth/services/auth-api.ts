import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';

import { environment } from '../../../../environments/environment';
import {
  CurrentUserResponse,
  LoginRequest,
  LoginResponse,
  MessageResponse,
  RegisterRequest,
  RegisterResponse,
  ChangePasswordResponse
} from '../models/auth.models';

@Injectable({
  providedIn: 'root'
})
export class AuthApi {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiUrl;

  register(request: RegisterRequest) {
    return this.http.post<RegisterResponse>(
      `${this.apiUrl}/api/auth/register`,
      request
    );
  }

  login(request: LoginRequest) {
    return this.http.post<LoginResponse>(
      `${this.apiUrl}/api/auth/login`,
      request
    );
  }

  getCurrentUser() {
    return this.http.get<CurrentUserResponse>(
      `${this.apiUrl}/api/auth/me`
    );
  }

  confirmEmail(token: string) {
    return this.http.post<MessageResponse>(
      `${this.apiUrl}/api/auth/confirm-email`,
      { token }
    );
  }

  resendConfirmationEmail(email: string) {
    return this.http.post<MessageResponse>(
      `${this.apiUrl}/api/auth/resend-confirmation-email`,
      { email }
    );
  }

  forgotPassword(email: string) {
    return this.http.post<MessageResponse>(
      `${this.apiUrl}/api/auth/forgot-password`,
      { email }
    );
  }

  resetPassword(token: string, newPassword: string) {
    return this.http.post<MessageResponse>(
      `${this.apiUrl}/api/auth/reset-password`,
      {
        token,
        newPassword
      }
    );
  }

  changePassword(currentPassword: string, newPassword: string) {
    return this.http.post<ChangePasswordResponse>(
      `${this.apiUrl}/api/auth/change-password`,
      {
        currentPassword,
        newPassword
      }
    );
  }
}