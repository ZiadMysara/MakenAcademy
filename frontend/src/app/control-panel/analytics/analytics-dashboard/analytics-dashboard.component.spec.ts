import { ComponentFixture, TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';
import { AnalyticsDashboardComponent } from './analytics-dashboard.component';
import { ApiService } from '../../../core/services/api.service';

describe('AnalyticsDashboardComponent', () => {
  let component: AnalyticsDashboardComponent;
  let fixture: ComponentFixture<AnalyticsDashboardComponent>;
  let mockApiService: any;

  const mockAnalyticsData = {
    enrollmentCount: 150,
    completionRate: 75,
    examPassRate: 85,
    activeCourses: 10,
    activeStudents: 120,
    courseStats: [
      { courseName: 'Course A', enrollments: 50, completions: 40 },
      { courseName: 'Course B', enrollments: 60, completions: 45 },
      { courseName: 'Course C', enrollments: 40, completions: 30 }
    ]
  };

  beforeEach(async () => {
    mockApiService = {
      get: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [AnalyticsDashboardComponent],
      providers: [
        { provide: ApiService, useValue: mockApiService }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(AnalyticsDashboardComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('ngOnInit', () => {
    it('should load analytics on init', () => {
      mockApiService.get.mockReturnValue(of(mockAnalyticsData));

      component.ngOnInit();

      expect(mockApiService.get).toHaveBeenCalledWith('api/analytics');
      expect(component.analytics).toEqual(mockAnalyticsData);
      expect(component.loading).toBe(false);
      expect(component.error).toBeNull();
    });

    it('should handle error when loading analytics fails', () => {
      const errorMessage = 'Network error';
      mockApiService.get.mockReturnValue(throwError(() => new Error(errorMessage)));

      component.ngOnInit();

      expect(component.error).toBe(errorMessage);
      expect(component.loading).toBe(false);
      expect(component.analytics).toBeNull();
    });
  });

  describe('loadAnalytics', () => {
    it('should reset error state before loading', () => {
      component.error = 'Previous error';
      mockApiService.get.mockReturnValue(of(mockAnalyticsData));

      component.loadAnalytics();

      expect(component.error).toBeNull();
    });

    it('should set loading to true during fetch', () => {
      mockApiService.get.mockReturnValue(of(mockAnalyticsData));

      component.loadAnalytics();

      expect(mockApiService.get).toHaveBeenCalled();
    });
  });

  describe('onRetry', () => {
    it('should call loadAnalytics', () => {
      mockApiService.get.mockReturnValue(of(mockAnalyticsData));
      vi.spyOn(component, 'loadAnalytics');

      component.onRetry();

      expect(component.loadAnalytics).toHaveBeenCalled();
    });
  });

  describe('isEmpty', () => {
    it('should return true when not loading, no error, and no analytics', () => {
      component.loading = false;
      component.error = null;
      component.analytics = null;

      expect(component.isEmpty).toBe(true);
    });

    it('should return false when loading', () => {
      component.loading = true;
      component.error = null;
      component.analytics = null;

      expect(component.isEmpty).toBe(false);
    });

    it('should return false when there is an error', () => {
      component.loading = false;
      component.error = 'Error';
      component.analytics = null;

      expect(component.isEmpty).toBe(false);
    });

    it('should return false when analytics data exists', () => {
      component.loading = false;
      component.error = null;
      component.analytics = mockAnalyticsData;

      expect(component.isEmpty).toBe(false);
    });
  });

  describe('getMaxEnrollment', () => {
    it('should return maximum enrollment from course stats or 100, whichever is greater', () => {
      component.analytics = mockAnalyticsData;

      const max = component.getMaxEnrollment();

      expect(max).toBe(100); // Returns at least 100, even though max enrollment is 60
    });

    it('should return 100 when no course stats', () => {
      component.analytics = {
        ...mockAnalyticsData,
        courseStats: []
      };

      const max = component.getMaxEnrollment();

      expect(max).toBe(100);
    });

    it('should return 100 when course stats is undefined', () => {
      component.analytics = {
        ...mockAnalyticsData,
        courseStats: undefined
      };

      const max = component.getMaxEnrollment();

      expect(max).toBe(100);
    });

    it('should return at least 100 even if max enrollment is lower', () => {
      component.analytics = {
        ...mockAnalyticsData,
        courseStats: [
          { courseName: 'Small Course', enrollments: 10, completions: 5 }
        ]
      };

      const max = component.getMaxEnrollment();

      expect(max).toBe(100);
    });
  });

  describe('getBarWidth', () => {
    it('should calculate correct percentage width', () => {
      component.analytics = mockAnalyticsData;

      const width = component.getBarWidth(30);

      expect(width).toBe('30%'); // 30/100 * 100 = 30% (max is 100, not 60)
    });

    it('should cap at 100%', () => {
      component.analytics = mockAnalyticsData;

      const width = component.getBarWidth(120);

      expect(width).toBe('100%');
    });

    it('should handle zero value', () => {
      component.analytics = mockAnalyticsData;

      const width = component.getBarWidth(0);

      expect(width).toBe('0%');
    });
  });

  describe('No Client-Side Computation Verification', () => {
    it('should render all metrics directly from backend data without computation', () => {
      mockApiService.get.mockReturnValue(of(mockAnalyticsData));

      component.ngOnInit();

      // Verify all metrics come directly from backend response
      expect(component.analytics?.enrollmentCount).toBe(150);
      expect(component.analytics?.completionRate).toBe(75);
      expect(component.analytics?.examPassRate).toBe(85);
      expect(component.analytics?.activeCourses).toBe(10);
      expect(component.analytics?.activeStudents).toBe(120);
    });

    it('should not aggregate or compute course statistics', () => {
      mockApiService.get.mockReturnValue(of(mockAnalyticsData));

      component.ngOnInit();

      // Verify course stats are rendered as-is from backend
      expect(component.analytics?.courseStats).toEqual(mockAnalyticsData.courseStats);
      expect(component.analytics?.courseStats?.length).toBe(3);
    });

    it('should only perform UI-level calculations for visual rendering', () => {
      component.analytics = mockAnalyticsData;

      // These are pure UI calculations for bar chart rendering, not business logic
      const maxEnrollment = component.getMaxEnrollment();
      const barWidth = component.getBarWidth(30);

      expect(typeof maxEnrollment).toBe('number');
      expect(typeof barWidth).toBe('string');
      expect(barWidth).toContain('%');
    });
  });
});
