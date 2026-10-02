import { Component, inject, signal, OnInit } from '@angular/core';
import { DatePipe } from '@angular/common';
import { DashboardService } from '../../core/services/dashboard.service';
import { DashboardStats } from '../../core/models/models';

@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [DatePipe],
  template: `
    <div class="page">
      <div class="page-header">
        <h1>📊 Dashboard</h1>
        <p class="subtitle">E-Commerce Analytics</p>
      </div>

      @if (loading()) {
        <div class="loading">Loading analytics...</div>
      } @else if (error()) {
        <div class="alert error">{{ error() }}</div>
      } @else if (stats()) {
        <div class="stats-grid">
          <div class="stat-card blue">
            <div class="stat-icon">👥</div>
            <div class="stat-body">
              <div class="stat-value">{{ stats()!.totalCustomers }}</div>
              <div class="stat-label">Total Customers</div>
            </div>
          </div>
          <div class="stat-card purple">
            <div class="stat-icon">📦</div>
            <div class="stat-body">
              <div class="stat-value">{{ stats()!.totalOrders }}</div>
              <div class="stat-label">Total Orders</div>
            </div>
          </div>
          <div class="stat-card green">
            <div class="stat-icon">💰</div>
            <div class="stat-body">
              <div class="stat-value">\${{ stats()!.totalRevenue.toFixed(2) }}</div>
              <div class="stat-label">Total Revenue</div>
            </div>
          </div>
          <div class="stat-card orange">
            <div class="stat-icon">📈</div>
            <div class="stat-body">
              <div class="stat-value">\${{ stats()!.averageOrderValue.toFixed(2) }}</div>
              <div class="stat-label">Avg Order Value</div>
            </div>
          </div>
        </div>

        <div class="order-status">
          <div class="status-card pending">
            <span class="status-icon">⏳</span>
            <span class="status-count">{{ stats()!.pendingOrders }}</span>
            <span class="status-label">Pending</span>
          </div>
          <div class="status-card completed">
            <span class="status-icon">✅</span>
            <span class="status-count">{{ stats()!.completedOrders }}</span>
            <span class="status-label">Completed</span>
          </div>
        </div>

        <div class="top-customers">
          <h2>🏆 Top Customers</h2>
          <table class="data-table">
            <thead>
              <tr><th>Customer</th><th>Orders</th><th>Total Spent</th></tr>
            </thead>
            <tbody>
              @for (cust of stats()!.topCustomers; track cust.customerId) {
                <tr>
                  <td>{{ cust.customerName }}</td>
                  <td><span class="badge">{{ cust.ordersCount }}</span></td>
                  <td class="amount">\${{ cust.totalSpent.toFixed(2) }}</td>
                </tr>
              }
            </tbody>
          </table>
        </div>

        <p class="generated-at">Generated at {{ stats()!.generatedAtUtc | date:'medium' }}</p>
      }
    </div>
  `,
  styles: [`
    .page { padding: 2rem; max-width: 1200px; margin: 0 auto; }
    .page-header { margin-bottom: 2rem; }
    h1 { color: #e94560; margin: 0; font-size: 2rem; }
    .subtitle { color: #888; margin: .25rem 0 0; }
    .stats-grid { display: grid; grid-template-columns: repeat(auto-fill, minmax(220px, 1fr)); gap: 1.5rem; margin-bottom: 2rem; }
    .stat-card { background: #1a1a2e; border-radius: 10px; padding: 1.5rem; border: 1px solid #2a2a4a; display: flex; align-items: center; gap: 1.25rem; }
    .stat-icon { font-size: 2rem; }
    .stat-value { font-size: 1.75rem; font-weight: 700; color: #fff; }
    .stat-label { color: #888; font-size: .85rem; margin-top: .25rem; }
    .stat-card.blue { border-left: 4px solid #3b82f6; }
    .stat-card.purple { border-left: 4px solid #8b5cf6; }
    .stat-card.green { border-left: 4px solid #22c55e; }
    .stat-card.orange { border-left: 4px solid #f59e0b; }
    .order-status { display: flex; gap: 1rem; margin-bottom: 2rem; flex-wrap: wrap; }
    .status-card { background: #1a1a2e; border-radius: 10px; padding: 1.25rem 2rem; display: flex; align-items: center; gap: 1rem; border: 1px solid #2a2a4a; flex: 1; min-width: 150px; }
    .status-icon { font-size: 1.5rem; }
    .status-count { font-size: 1.5rem; font-weight: 700; }
    .status-card.pending .status-count { color: #f59e0b; }
    .status-card.completed .status-count { color: #22c55e; }
    .status-label { color: #888; font-size: .85rem; }
    .top-customers { background: #1a1a2e; border-radius: 10px; padding: 1.5rem; border: 1px solid #2a2a4a; margin-bottom: 1.5rem; }
    h2 { color: #fff; margin: 0 0 1.25rem; font-size: 1.2rem; }
    .data-table { width: 100%; border-collapse: collapse; }
    .data-table th { color: #888; font-size: .8rem; font-weight: 600; text-transform: uppercase; letter-spacing: .05em; padding: .5rem .75rem; text-align: left; border-bottom: 1px solid #2a2a4a; }
    .data-table td { color: #ccc; padding: .75rem .75rem; border-bottom: 1px solid #1e1e3a; font-size: .9rem; }
    .data-table tr:last-child td { border: none; }
    .badge { background: rgba(233,69,96,.15); color: #e94560; padding: 2px 8px; border-radius: 12px; font-size: .8rem; font-weight: 600; }
    .amount { color: #22c55e; font-weight: 600; }
    .loading { color: #888; text-align: center; padding: 3rem; }
    .alert { padding: .75rem 1rem; border-radius: 6px; margin-bottom: 1rem; }
    .alert.error { background: rgba(233,69,96,.15); border: 1px solid #e94560; color: #e94560; }
    .generated-at { color: #555; font-size: .8rem; text-align: right; }
  `]
})
export class DashboardComponent implements OnInit {
  private dashboardService = inject(DashboardService);
  stats = signal<DashboardStats | null>(null);
  loading = signal(true);
  error = signal('');

  ngOnInit() {
    this.dashboardService.getStats().subscribe({
      next: s => { this.stats.set(s); this.loading.set(false); },
      error: () => { this.error.set('Failed to load dashboard stats.'); this.loading.set(false); }
    });
  }
}
