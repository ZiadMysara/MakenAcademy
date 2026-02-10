import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CourseListComponent } from './course-list.component';
import { ApiService } from '../../../core/services/api.service';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

describe('CourseListComponent', () => {
  let component: CourseListComponent;
  let fixture: ComponentFixture<CourseListComponent>;
  let mockApiService: any;
  let mockRouter: any;

  const mockCourses = [
    {
      id: '1',
      name: 'Introduction to Islam',
      description: 'Basic principles of Islam',
      isLocked: false,
      progress: 50,
      lessonCount: 10,
      completedLessonCount: 5
    },
    {
      id: '2',
      name: 'Advanced Fiqh',
      description: 'Advanced Islamic jurisprudence',
      isLocked: true,
      lessonCount: 15,
      completedLessonCount: 0
    }
  ];

  beforeEach(async () => {
    mockApiService = {
      get: vi.fn()
    };

    mockRouter = {
      navigate: vi.fn()
    };

    await TestBed.configureTestingModule({
      imports: [CourseListComponent],
      providers: [
        { provide: ApiService, useValue: mockApiService },
        { provide: Router, useValue: mockRouter }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CourseListComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Loading State', () => {
    it('should hide loading state after data loads', async () => {
      mockApiService.get.mockReturnValue(of(mockCourses));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.loading).toBe(false);
      expect(component.courses).toEqual(mockCourses);
    });
  });

  describe('Data Loading', () => {
    it('should fetch courses from API on init', () => {
      mockApiService.get.mockReturnValue(of(mockCourses));
      
      component.ngOnInit();
      
      expect(mockApiService.get).toHaveBeenCalledWith('api/courses');
    });

    it('should populate courses array with API data', async () => {
      mockApiService.get.mockReturnValue(of(mockCourses));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.courses).toEqual(mockCourses);
      expect(component.courses.length).toBe(2);
    });

    it('should handle empty courses list', async () => {
      mockApiService.get.mockReturnValue(of([]));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.courses).toEqual([]);
      expect(component.isEmpty).toBe(true);
    });
  });

  describe('Error Handling', () => {
    it('should show error state when API fails', async () => {
      const error = new Error('Network error');
      mockApiService.get.mockReturnValue(throwError(() => error));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.error).toBe('Network error');
      expect(component.loading).toBe(false);
    });

    it('should show default error message when error has no message', async () => {
      mockApiService.get.mockReturnValue(throwError(() => ({})));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.error).toBe('Failed to load courses');
    });

    it('should retry loading on retry', () => {
      mockApiService.get.mockReturnValue(of(mockCourses));
      
      component.onRetry();
      
      expect(mockApiService.get).toHaveBeenCalledWith('api/courses');
    });
  });

  describe('Course Navigation', () => {
    it('should navigate to course detail when unlocked course is clicked', () => {
      const unlockedCourse = mockCourses[0];
      
      component.onCourseClick(unlockedCourse);
      
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/courses', '1']);
    });

    it('should not navigate when locked course is clicked', () => {
      const lockedCourse = mockCourses[1];
      
      component.onCourseClick(lockedCourse);
      
      expect(mockRouter.navigate).not.toHaveBeenCalled();
    });
  });

  describe('Progress Calculation', () => {
    it('should calculate progress percentage correctly', () => {
      const course = mockCourses[0];
      
      const percentage = component.getProgressPercentage(course);
      
      expect(percentage).toBe(50);
    });

    it('should return 0 when lesson count is 0', () => {
      const course = { ...mockCourses[0], lessonCount: 0 };
      
      const percentage = component.getProgressPercentage(course);
      
      expect(percentage).toBe(0);
    });

    it('should return 0 when lesson count is undefined', () => {
      const course = { ...mockCourses[0], lessonCount: undefined };
      
      const percentage = component.getProgressPercentage(course);
      
      expect(percentage).toBe(0);
    });

    it('should handle missing completedLessonCount', () => {
      const course = { ...mockCourses[0], completedLessonCount: undefined };
      
      const percentage = component.getProgressPercentage(course);
      
      expect(percentage).toBe(0);
    });
  });

  describe('Empty State', () => {
    it('should show empty state when no courses and not loading', async () => {
      mockApiService.get.mockReturnValue(of([]));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.isEmpty).toBe(true);
    });

    it('should not show empty state when loading', () => {
      component.loading = true;
      component.courses = [];
      
      expect(component.isEmpty).toBe(false);
    });

    it('should not show empty state when error exists', () => {
      component.error = 'Error';
      component.courses = [];
      
      expect(component.isEmpty).toBe(false);
    });
  });
});
