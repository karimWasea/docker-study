import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';
import { AuthService } from '../../../core/services/auth.service';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [FormsModule, RouterLink],
  template: `
    <div class="auth-container">
      <div class="auth-card">
        <h2>🔐 Login</h2>
        <p class="subtitle">Sign in to your E-Commerce account</p>

        @if (error()) {
          <div class="alert error">{{ error() }}</div>
        }

        <form (ngSubmit)="login()">
          <div class="form-group">
            <label>Email</label>
            <input type="email" [(ngModel)]="email" name="email" placeholder="admin@local.test" required />
          </div>
          <div class="form-group">
            <label>Password</label>
            <input type="password" [(ngModel)]="password" name="password" placeholder="••••••••" required />
          </div>
          <button type="submit" class="btn-primary" [disabled]="loading()">
            {{ loading() ? 'Signing in...' : 'Sign In' }}
          </button>
        </form>

        <p class="auth-footer">
          No account? <a routerLink="/register">Register here</a>
        </p>

        <div class="demo-creds">
          <small>🧑‍💼 Admin: admin&#64;local.test / Admin#12345</small>
        </div>
      </div>
    </div>
  `,
  styles: [`
    .auth-container { min-height: 100vh; display: flex; align-items: center; justify-content: center; background: #0f0f23; }
    .auth-card { background: #1a1a2e; padding: 2.5rem; border-radius: 12px; width: 100%; max-width: 420px; box-shadow: 0 8px 32px rgba(0,0,0,0.4); }
    h2 { color: #e94560; margin: 0 0 .5rem; font-size: 1.8rem; }
    .subtitle { color: #888; margin: 0 0 2rem; font-size: .9rem; }
    .form-group { margin-bottom: 1.2rem; }
    label { display: block; color: #ccc; font-size: .85rem; margin-bottom: .4rem; }
    input { width: 100%; padding: .75rem 1rem; background: #16213e; border: 1px solid #333; border-radius: 6px; color: #fff; font-size: .95rem; box-sizing: border-box; outline: none; transition: border .2s; }
    input:focus { border-color: #e94560; }
    .btn-primary { width: 100%; padding: .85rem; background: #e94560; color: white; border: none; border-radius: 6px; font-size: 1rem; font-weight: 600; cursor: pointer; margin-top: .5rem; transition: background .2s; }
    .btn-primary:hover:not(:disabled) { background: #c73652; }
    .btn-primary:disabled { opacity: .6; cursor: not-allowed; }
    .alert { padding: .75rem 1rem; border-radius: 6px; margin-bottom: 1rem; font-size: .9rem; }
    .alert.error { background: rgba(233,69,96,.15); border: 1px solid #e94560; color: #e94560; }
    .auth-footer { text-align: center; color: #888; margin-top: 1.5rem; font-size: .9rem; }
    .auth-footer a { color: #e94560; text-decoration: none; }
    .demo-creds { margin-top: 1rem; text-align: center; padding: .75rem; background: rgba(233,69,96,.08); border-radius: 6px; }
    .demo-creds small { color: #aaa; font-size: .8rem; }
  `]
})
export class LoginComponent {
  private authService = inject(AuthService);
  private router = inject(Router);

  email = '';
  password = '';
  loading = signal(false);
  error = signal('');

  login() {
    this.loading.set(true);
    this.error.set('');

    this.authService.login({ email: this.email, password: this.password }).subscribe({
      next: () => this.router.navigate(['/products']),
      error: () => {
        this.error.set('Invalid email or password. Please try again.');
        this.loading.set(false);
      }
    });
  }
}
