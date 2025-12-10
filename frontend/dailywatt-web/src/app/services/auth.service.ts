import { Injectable, computed, signal } from "@angular/core";
import { HttpClient } from "@angular/common/http";
import { map, tap } from "rxjs";
import { environment } from "../../environments/environment";
import {
  ChangePasswordRequest,
  LoginRequest,
  RegisterRequest,
  UpdateProfileRequest,
  UserProfile,
} from "../models/auth.models";

@Injectable({ providedIn: "root" })
export class AuthService {
  private tokenKey = "dailywatt_token";
  private authState = signal<boolean>(this.hasValidToken());

  // Computed signal for logged-in status
  isLoggedIn = computed(() => this.authState());

  constructor(private http: HttpClient) {}

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
    localStorage.removeItem(this.tokenKey);
    this.authState.set(false);
  }

  get token(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  get isLoggedIn$() {
    return this.authState();
  }

  private setToken(token: string) {
    localStorage.setItem(this.tokenKey, token);
    this.authState.set(true);
  }

  private hasValidToken() {
    const token = localStorage.getItem(this.tokenKey);
    if (!token) {
      return false;
    }

    const payload = this.decodeJwt(token);
    if (!payload?.exp) {
      localStorage.removeItem(this.tokenKey);
      return false;
    }

    const expiresAt = payload.exp * 1000;
    const isValid = Date.now() < expiresAt;
    if (!isValid) {
      localStorage.removeItem(this.tokenKey);
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

  hasValidSession(): boolean {
    const valid = this.hasValidToken();
    this.authState.set(valid);
    return valid;
  }
}
