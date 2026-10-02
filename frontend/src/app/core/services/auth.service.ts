import { Injectable, signal, computed } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthResponse, LoginRequest, RegisterRequest } from '../models/models';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly baseUrl = `${environment.apiUrl}/api/auth`;
  private readonly TOKEN_KEY = 'docker_study_token';
  private readonly USER_KEY = 'docker_study_user';

  private currentTokenSignal = signal<string | null>(this.getStoredToken());
  private currentUserSignal = signal<{ email: string; roles: string[] } | null>(this.getStoredUser());

  readonly token = computed(() => this.currentTokenSignal());
  readonly currentUser = computed(() => this.currentUserSignal());
  readonly isAuthenticated = computed(() => !!this.currentTokenSignal());
  readonly isAdmin = computed(() => this.currentUserSignal()?.roles?.includes('Admin') ?? false);

  constructor(private http: HttpClient) {}

  login(request: LoginRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/login`, request).pipe(
      tap(res => this.handleAuthSuccess(res))
    );
  }

  register(request: RegisterRequest): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.baseUrl}/register`, request).pipe(
      tap(res => this.handleAuthSuccess(res))
    );
  }

  logout(): void {
    localStorage.removeItem(this.TOKEN_KEY);
    localStorage.removeItem(this.USER_KEY);
    this.currentTokenSignal.set(null);
    this.currentUserSignal.set(null);
  }

  private handleAuthSuccess(res: AuthResponse): void {
    localStorage.setItem(this.TOKEN_KEY, res.token);
    const user = { email: res.email, roles: res.roles };
    localStorage.setItem(this.USER_KEY, JSON.stringify(user));
    this.currentTokenSignal.set(res.token);
    this.currentUserSignal.set(user);
  }

  private getStoredToken(): string | null {
    if (typeof localStorage === 'undefined') return null;
    return localStorage.getItem(this.TOKEN_KEY);
  }

  private getStoredUser(): { email: string; roles: string[] } | null {
    if (typeof localStorage === 'undefined') return null;
    const userStr = localStorage.getItem(this.USER_KEY);
    if (!userStr) return null;
    try {
      return JSON.parse(userStr);
    } catch {
      return null;
    }
  }
}
