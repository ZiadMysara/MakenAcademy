import { ComponentFixture, TestBed } from '@angular/core/testing';
import { CourseDetailComponent } from './course-detail.component';
import { ApiService } from '../../../core/services/api.service';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

describe('CourseDetailComponent', () => {
  let component: CourseDetailComponent;
  let fixture: ComponentFixture<CourseDetailComponent>;
  let mockApiService: any;
  let mockRouter: any;
  let mockActivatedRoute: any;

  const mockCourseDetail = {
    id: '1',
    name: 'Introduction to Islam',
    description: 'Basic principles of Islam',
    isLocked: false,
    progress: 50,
    lessons: [
      {
        id: 'l1',
        title: 'Lesson 1: Foundations',
        description: 'Introduction to Islamic foundations',
        order: 1,
        isLocked: false,
        isCompleted: true,
        contentType: 'video' as const,
        duration: '15 min'
      },
      {
        id: 'l2',
        title: 'Lesson 2: Pillars',
        description: 'The five pillars of Islam',
        order: 2,
        isLocked: false,
        isCompleted: false,
        contentType: 'pdf' as const,
        duration: '20 min'
      },
      {
        id: 'l3',
        title: 'Lesson 3: Advanced Topics',
        description: 'Advanced Islamic concepts',
        order: 3,
        isLocked: true,
        isCompleted: false,
        contentType: 'video' as const
      }
    ]
  };

  beforeEach(async () => {
    mockApiService = {
      get: vi.fn()
    };

    mockRouter = {
      navigate: vi.fn()
    };

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: vi.fn().mockReturnValue('1')
        }
      }
    };

    await TestBed.configureTestingModule({
      imports: [CourseDetailComponent],
      providers: [
        { provide: ApiService, useValue: mockApiService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CourseDetailComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Initialization', () => {
    it('should load course detail on init with course ID from route', () => {
      mockApiService.get.mockReturnValue(of(mockCourseDetail));
      
      component.ngOnInit();
      
      expect(mockApiService.get).toHaveBeenCalledWith('api/courses/1');
    });

    it('should set error when course ID is not provided', () => {
      mockActivatedRoute.snapshot.paramMap.get.mockReturnValue(null);
      
      component.ngOnInit();
      
      expect(component.error).toBe('Course ID not provided');
    });
  });

  describe('Loading State', () => {
    it('should hide loading state after data loads', async () => {
      mockApiService.get.mockReturnValue(of(mockCourseDetail));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.loading).toBe(false);
      expect(component.course).toEqual(mockCourseDetail);
    });
  });

  describe('Data Loading', () => {
    it('should populate course with API data', async () => {
      mockApiService.get.mockReturnValue(of(mockCourseDetail));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.course).toEqual(mockCourseDetail);
    });

    it('should load lessons from course detail', async () => {
      mockApiService.get.mockReturnValue(of(mockCourseDetail));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.course?.lessons).toEqual(mockCourseDetail.lessons);
      expect(component.course?.lessons.length).toBe(3);
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
      
      expect(component.error).toBe('Failed to load course details');
    });

    it('should retry loading on retry', () => {
      mockApiService.get.mockReturnValue(of(mockCourseDetail));
      
      component.onRetry();
      
      expect(mockApiService.get).toHaveBeenCalledWith('api/courses/1');
    });
  });

  describe('Lesson Navigation', () => {
    beforeEach(async () => {
      mockApiService.get.mockReturnValue(of(mockCourseDetail));
      component.ngOnInit();
      await fixture.whenStable();
    });

    it('should navigate to lesson viewer when unlocked lesson is clicked', () => {
      const unlockedLesson = mockCourseDetail.lessons[0];
      
      component.onLessonClick(unlockedLesson);
      
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/courses', '1', 'lessons', 'l1']);
    });

    it('should not navigate when locked lesson is clicked', () => {
      const lockedLesson = mockCourseDetail.lessons[2];
      
      component.onLessonClick(lockedLesson);
      
      expect(mockRouter.navigate).not.toHaveBeenCalled();
    });
  });

  describe('Back Navigation', () => {
    it('should navigate back to course list', () => {
      component.onBackClick();
      
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/courses']);
    });
  });

  describe('Lesson Icon', () => {
    it('should return lock icon for locked lesson', () => {
      const lockedLesson = mockCourseDetail.lessons[2];
      
      const icon = component.getLessonIcon(lockedLesson);
      
      expect(icon).toBe('🔒');
    });

    it('should return checkmark for completed lesson', () => {
      const completedLesson = mockCourseDetail.lessons[0];
      
      const icon = component.getLessonIcon(completedLesson);
      
      expect(icon).toBe('✓');
    });

    it('should return video icon for unlocked video lesson', () => {
      const videoLesson = { ...mockCourseDetail.lessons[1], contentType: 'video' as const };
      
      const icon = component.getLessonIcon(videoLesson);
      
      expect(icon).toBe('🎥');
    });

    it('should return PDF icon for unlocked PDF lesson', () => {
      const pdfLesson = mockCourseDetail.lessons[1];
      
      const icon = component.getLessonIcon(pdfLesson);
      
      expect(icon).toBe('📄');
    });
  });

  describe('Lesson Status', () => {
    it('should return "Locked" for locked lesson', () => {
      const lockedLesson = mockCourseDetail.lessons[2];
      
      const status = component.getLessonStatus(lockedLesson);
      
      expect(status).toBe('Locked');
    });

    it('should return "Completed" for completed lesson', () => {
      const completedLesson = mockCourseDetail.lessons[0];
      
      const status = component.getLessonStatus(completedLesson);
      
      expect(status).toBe('Completed');
    });

    it('should return "Available" for unlocked, incomplete lesson', () => {
      const availableLesson = mockCourseDetail.lessons[1];
      
      const status = component.getLessonStatus(availableLesson);
      
      expect(status).toBe('Available');
    });
  });

  describe('Empty State', () => {
    it('should show empty state when course is null and not loading', () => {
      component.loading = false;
      component.error = null;
      component.course = null;
      
      expect(component.isEmpty).toBe(true);
    });

    it('should not show empty state when loading', () => {
      component.loading = true;
      component.course = null;
      
      expect(component.isEmpty).toBe(false);
    });

    it('should not show empty state when error exists', () => {
      component.error = 'Error';
      component.course = null;
      
      expect(component.isEmpty).toBe(false);
    });
  });
});
