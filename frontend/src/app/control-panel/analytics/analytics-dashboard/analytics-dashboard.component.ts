import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { ApiService } from '../../../core/services/api.service';
import { StateContainerComponent } from '../../../shared/components/state-container/state-container.component';

interface AnalyticsData {
  enrollmentCount: number;
  completionRate: number;
  examPassRate: number;
  activeCourses: number;
  activeStudents: number;
  courseStats?: Array<{
    courseName: string;
    enrollments: number;
    completions: number;
  }>;
}

/**
 * AnalyticsDashboardComponent
 * 
 * Displays simple descriptive analytics for CompanyAdmin.
 * All metrics are pre-computed by backend - no client-side aggregation.
 * 
 * Constitution Compliance:
 * - [FEAT-007] All UI behavior driven by backend API responses
 * - [FEAT-013] No derived business state computed client-side
 * - [FEAT-021] Manages only local UI state (loading, error)
 * - [FEAT-038] No business logic
 * - Constitution §11.II: Simple, descriptive, non-predictive analytics only
 * 
 * US5: Analytics Dashboard (Priority: P3)
 */
@Component({
  selector: 'app-analytics-dashboard',
  standalone: true,
  imports: [CommonModule, StateContainerComponent],
  templateUrl: './analytics-dashboard.component.html',
  styleUrl: './analytics-dashboard.component.css'
})
export class AnalyticsDashboardComponent implements OnInit {
  loading = false;
  error: string | null = null;
  analytics: AnalyticsData | null = null;

  constructor(private apiService: ApiService) {}

  ngOnInit(): void {
    this.loadAnalytics();
  }

  loadAnalytics(): void {
    this.loading = true;
    this.error = null;

    this.apiService.get<AnalyticsData>('api/analytics')
      .subscribe({
        next: (data) => {
          this.analytics = data;
          this.loading = false;
        },
        error: (err) => {
          this.error = err.message || 'Failed to load analytics';
          this.loading = false;
        }
      });
  }

  onRetry(): void {
    this.loadAnalytics();
  }

  get isEmpty(): boolean {
    return !this.loading && !this.error && this.analytics === null;
  }

  /**
   * Gets the maximum enrollment count for chart scaling
   * Used only for visual rendering, not business logic
   */
  getMaxEnrollment(): number {
    if (!this.analytics?.courseStats || this.analytics.courseStats.length === 0) {
      return 100;
    }
    return Math.max(...this.analytics.courseStats.map(c => c.enrollments), 100);
  }

  /**
   * Calculates bar width percentage for visual display
   * Pure UI calculation, not business logic
   */
  getBarWidth(value: number): string {
    const max = this.getMaxEnrollment();
    const percentage = (value / max) * 100;
    return `${Math.min(percentage, 100)}%`;
  }
}
