import { Injectable } from "@angular/core";

/**
 * Simple service to provide token access without triggering AuthService initialization
 * Avoids circular dependency during HttpClient bootstrap
 */
@Injectable({ providedIn: "root" })
export class AuthTokenService {
  private tokenKey = "dailywatt_token";

  getToken(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  setToken(token: string): void {
    localStorage.setItem(this.tokenKey, token);
  }

  clearToken(): void {
    localStorage.removeItem(this.tokenKey);
  }
}
