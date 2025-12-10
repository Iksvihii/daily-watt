import { Injectable, computed, inject, signal } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { map, tap } from "rxjs";
import { environment } from "../../environments/environment";
import { AuthTokenService } from "./auth-token.service";
import {
  ChangePasswordRequest,
  LoginRequest,
  RegisterRequest,
  UpdateProfileRequest,
  UserProfile,
} from "../models/auth.models";

@Injectable({ providedIn: "root" })
export class AuthService {
  private http = inject(HttpClient);
  private tokenService = inject(AuthTokenService);
  private authState = signal<boolean>(false);

  // Computed signal for logged-in status
  isLoggedIn = computed(() => this.authState());

  constructor() {
    // Do not call hasValidToken() during construction
    // State will be verified on first access or after explicit login/logout
  }

  login(credentials: LoginRequest) {
    return this.http
      .post<string>(`${environment.apiUrl}/api/auth/login`, credentials)
      .pipe(
        tap((token: string) => this.setToken(token)),
        map(() => void 0)
      );
  }

  register(credentials: RegisterRequest) {
    return this.http
      .post<string>(`${environment.apiUrl}/api/auth/register`, credentials)
      .pipe(
        tap((token: string) => this.setToken(token)),
        map(() => void 0)
      );
  }

  getProfile() {
    return this.http.get<UserProfile>(`${environment.apiUrl}/api/auth/me`);
  }

  updateProfile(request: UpdateProfileRequest) {
    return this.http.put<void>(
      `${environment.apiUrl}/api/auth/profile`,
      request
    );
  }

  changePassword(request: ChangePasswordRequest) {
    return this.http.post<void>(
      `${environment.apiUrl}/api/auth/change-password`,
      request
    );
  }

  logout() {
    this.tokenService.clearToken();
    this.authState.set(false);
  }

  get token(): string | null {
    return this.tokenService.getToken();
  }

  get isLoggedIn$() {
    return this.authState();
  }

  hasValidSession(): boolean {
    const valid = this.hasValidToken();
    this.authState.set(valid);
    return valid;
  }

  private setToken(token: string) {
    this.tokenService.setToken(token);
    this.authState.set(true);
  }

  private hasValidToken() {
    const token = this.tokenService.getToken();
    if (!token) {
      return false;
    }

    const payload = this.decodeJwt(token);
    if (!payload?.exp) {
      this.tokenService.clearToken();
      return false;
    }

    const expiresAt = payload.exp * 1000;
    const isValid = Date.now() < expiresAt;
    if (!isValid) {
      this.tokenService.clearToken();
    }
    return isValid;
  }

  private decodeJwt(token: string): { exp?: number } | null {
    try {
      const payload = token.split(".")[1];
      const decoded = atob(payload.replace(/-/g, "+").replace(/_/g, "/"));
      return JSON.parse(decoded);
    } catch {
      return null;
    }
  }
}
