import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Control Panel Dashboard Component
 * Placeholder for admin dashboard
 * Features will be defined in separate specs
 */
@Component({
  selector: 'app-dashboard',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="dashboard-container">
      <h1>Control Panel Dashboard</h1>
      <p>Admin and management features will be added here.</p>
    </div>
  `,
  styles: [`
    .dashboard-container {
      padding: 2rem;
    }

    h1 {
      font-size: 2rem;
      font-weight: 700;
      margin-bottom: 1rem;
      color: var(--color-text-primary, #111827);
    }

    p {
      color: var(--color-text-secondary, #6b7280);
    }
  `]
})
export class DashboardComponent {}
