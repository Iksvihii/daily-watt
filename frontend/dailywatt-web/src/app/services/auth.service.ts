import { Injectable } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { BehaviorSubject, map, tap } from 'rxjs';
import { environment } from '../../environments/environment';
import { AuthResponse, Credentials } from '../models/auth.models';

@Injectable({ providedIn: 'root' })
export class AuthService {
  private tokenKey = 'dailywatt_token';
  private authState = new BehaviorSubject<boolean>(this.hasToken());

  constructor(private http: HttpClient) {}

  login(credentials: Credentials) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/login`, credentials)
      .pipe(tap(res => this.setToken(res.token)), map(() => void 0));
  }

  register(credentials: Credentials) {
    return this.http
      .post<AuthResponse>(`${environment.apiUrl}/api/auth/register`, credentials)
      .pipe(tap(res => this.setToken(res.token)), map(() => void 0));
  }

  logout() {
    localStorage.removeItem(this.tokenKey);
    this.authState.next(false);
  }

  get token(): string | null {
    return localStorage.getItem(this.tokenKey);
  }

  get isLoggedIn$() {
    return this.authState.asObservable();
  }

  private setToken(token: string) {
    localStorage.setItem(this.tokenKey, token);
    this.authState.next(true);
  }

  private hasToken() {
    return !!localStorage.getItem(this.tokenKey);
  }
}
