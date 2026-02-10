import { Component, OnInit } from '@angular/core';
import { ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common';

/**
 * Error Page Component
 * Static error page displayed when critical failures occur
 * Constitution Rules: FE-019 (static error page, no retry)
 * 
 * Key Responsibilities:
 * - Display user-friendly error message
 * - Show error reason from query params
 * - No retry button (per Constitution clarification)
 * - Mobile-first responsive design
 * 
 * Usage:
 * Navigated to by guards or app initialization on critical failures:
 * ```
 * router.navigate(['/error'], { 
 *   queryParams: { reason: 'tenant-not-resolved' } 
 * });
 * ```
 */
@Component({
  selector: 'app-error-page',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './error-page.component.html',
  styleUrl: './error-page.component.css'
})
export class ErrorPageComponent implements OnInit {
  errorReason: string = 'unknown';
  errorMessage: string = 'Unable to load application';
  errorDetails: string = 'An unexpected error occurred. Please contact support if this issue persists.';

  constructor(private route: ActivatedRoute) {}

  ngOnInit(): void {
    // Read error reason from query params
    this.route.queryParams.subscribe(params => {
      this.errorReason = params['reason'] || 'unknown';
      this.updateErrorMessage();
    });
  }

  /**
   * Update error message based on reason
   */
  private updateErrorMessage(): void {
    switch (this.errorReason) {
      case 'tenant-not-resolved':
        this.errorMessage = 'Unable to load application';
        this.errorDetails = 'We could not identify your organization from the URL. Please check the URL and try again.';
        break;
      case 'api-unreachable':
        this.errorMessage = 'Service unavailable';
        this.errorDetails = 'We are unable to connect to our services. Please try again later.';
        break;
      case 'initialization-failed':
        this.errorMessage = 'Application failed to start';
        this.errorDetails = 'An error occurred while starting the application. Please contact support.';
        break;
      default:
        this.errorMessage = 'Unable to load application';
        this.errorDetails = 'An unexpected error occurred. Please contact support if this issue persists.';
    }
  }
}
