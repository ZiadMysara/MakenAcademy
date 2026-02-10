import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Loading Spinner Component
 * Displays a loading indicator
 * Constitution Rule: FE-033 (responsive design)
 */
@Component({
  selector: 'app-loading-spinner',
  standalone: true,
  imports: [CommonModule],
  template: `
    <div class="spinner-container">
      <div class="spinner"></div>
      <p class="spinner-text">Loading...</p>
    </div>
  `,
  styles: [`
    .spinner-container {
      display: flex;
      flex-direction: column;
      align-items: center;
      justify-content: center;
      padding: 2rem;
    }

    .spinner {
      width: 3rem;
      height: 3rem;
      border: 4px solid var(--color-secondary, #e5e7eb);
      border-top-color: var(--color-primary, #1E40AF);
      border-radius: 50%;
      animation: spin 1s linear infinite;
    }

    @keyframes spin {
      to { transform: rotate(360deg); }
    }

    .spinner-text {
      margin-top: 1rem;
      color: var(--color-text-secondary, #6b7280);
      font-size: 0.875rem;
    }
  `]
})
export class LoadingSpinnerComponent {}
