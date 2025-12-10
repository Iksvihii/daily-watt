import { Injectable, computed, signal } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { map, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthResponse, Credentials } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private tokenKey = 'dailywatt_token';
  private authState = signal<boolean>(this.hasToken());
  
  // Computed signal for logged-in status
  isLoggedIn = computed(() => this.authState());

  constructor(private http: HttpClient) {}

  login(credentials: Credentials) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/login`, credentials)
      .pipe(tap((res: AuthResponse) => this.setToken(res.token)), map(() => void 0));
  }

  register(credentials: Credentials) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/register`, credentials)
      .pipe(tap((res: AuthResponse) => this.setToken(res.token)), map(() => void 0));
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

  private hasToken() {
    return !!localStorage.getItem(this.tokenKey);
  }
}
