export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  username: string;
  password: string;
}

export interface UserProfile {
  email: string;
  username: string;
}

export interface UpdateProfileRequest {
  username: string;
}

export interface ChangePasswordRequest {
  currentPassword: string;
  newPassword: string;
}
