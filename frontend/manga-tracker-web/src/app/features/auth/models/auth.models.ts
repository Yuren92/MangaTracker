export interface RegisterRequest {
  email: string;
  password: string;
}

export interface RegisterResponse {
  userId: string;
  email: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface LoginResponse {
  accessToken: string;
}

export interface CurrentUserResponse {
  userId: string;
  email: string;
}

export interface MessageResponse {
  message: string;
}

// The password change revokes every previous token, including the current one.
export interface ChangePasswordResponse extends MessageResponse {
  accessToken: string;
}