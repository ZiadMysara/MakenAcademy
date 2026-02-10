import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Application Home Component
 * Placeholder for student home page
 * Features will be defined in separate specs
 */
@Component({
  selector: 'app-home',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="home-container">
      <h1>Welcome to Maken</h1>
      <p>Student-facing features will be added here.</p>
    </div>
  `,
  styles: [`
    .home-container {
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
export class HomeComponent {}
