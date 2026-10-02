import { Component, inject, signal } from '@angular/core';
import { Router, RouterLink, RouterLinkActive } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';

@Component({
  selector: 'app-navbar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive],
  template: `
    <nav class="navbar">
      <div class="navbar-brand">
        <a routerLink="/products">🛍️ Docker E-Shop</a>
      </div>
      @if (auth.isAuthenticated()) {
        <div class="navbar-links">
          <a routerLink="/products" routerLinkActive="active">Products</a>
          <a routerLink="/categories" routerLinkActive="active">Categories</a>
          @if (auth.isAdmin()) {
            <a routerLink="/dashboard" routerLinkActive="active">Dashboard</a>
          }
        </div>
      }
      <div class="navbar-user">
        @if (auth.isAuthenticated()) {
          <span class="user-email">{{ auth.currentUser()?.email }}</span>
          @if (auth.isAdmin()) {
            <span class="badge admin">Admin</span>
          }
          <button class="btn-logout" (click)="logout()">Logout</button>
        } @else {
          <a routerLink="/login">Login</a>
          <a routerLink="/register">Register</a>
        }
      </div>
    </nav>
  `,
  styles: [`
    .navbar {
      display: flex;
      align-items: center;
      background: #1a1a2e;
      padding: 0 2rem;
      height: 60px;
      gap: 2rem;
      box-shadow: 0 2px 8px rgba(0,0,0,0.3);
    }
    .navbar-brand a {
      color: #e94560;
      font-size: 1.3rem;
      font-weight: 700;
      text-decoration: none;
    }
    .navbar-links { display: flex; gap: 1.5rem; flex: 1; }
    .navbar-links a, .navbar-user a {
      color: #ccc;
      text-decoration: none;
      font-size: 0.95rem;
      transition: color .2s;
    }
    .navbar-links a:hover, .navbar-links a.active { color: #e94560; }
    .navbar-user { display: flex; align-items: center; gap: 1rem; margin-left: auto; }
    .user-email { color: #aaa; font-size: 0.85rem; }
    .badge { padding: 2px 8px; border-radius: 12px; font-size: 0.75rem; font-weight: 600; }
    .badge.admin { background: #e94560; color: white; }
    .btn-logout {
      background: transparent;
      border: 1px solid #e94560;
      color: #e94560;
      padding: 4px 12px;
      border-radius: 4px;
      cursor: pointer;
      font-size: 0.85rem;
      transition: all .2s;
    }
    .btn-logout:hover { background: #e94560; color: white; }
  `]
})
export class NavbarComponent {
  auth = inject(AuthService);
  private router = inject(Router);

  logout() {
    this.auth.logout();
    this.router.navigate(['/login']);
  }
}
