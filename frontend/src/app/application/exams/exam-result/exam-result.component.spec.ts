import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ExamResultComponent } from './exam-result.component';
import { ActivatedRoute, Router } from '@angular/router';
import { vi } from 'vitest';

describe('ExamResultComponent', () => {
  let component: ExamResultComponent;
  let fixture: ComponentFixture<ExamResultComponent>;
  let mockRouter: any;
  let mockActivatedRoute: any;

  const mockPassResult = {
    passed: true,
    score: 90,
    passingScore: 70,
    correctAnswers: 9,
    totalQuestions: 10
  };

  const mockFailResult = {
    passed: false,
    score: 50,
    passingScore: 70,
    correctAnswers: 5,
    totalQuestions: 10,
    feedback: 'Review the material and try again'
  };

  beforeEach(async () => {
    mockRouter = {
      navigate: vi.fn(),
      getCurrentNavigation: vi.fn()
    };

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: vi.fn((key: string) => {
            if (key === 'courseId') return 'c1';
            if (key === 'lessonId') return 'l1';
            return null;
          })
        }
      }
    };

    await TestBed.configureTestingModule({
      imports: [ExamResultComponent],
      providers: [
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();
  });

  describe('Pass Result', () => {
    beforeEach(() => {
      mockRouter.getCurrentNavigation.mockReturnValue({
        extras: { state: { result: mockPassResult } }
      });
      
      fixture = TestBed.createComponent(ExamResultComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('should create', () => {
      expect(component).toBeTruthy();
    });

    it('should display pass result from backend', () => {
      expect(component.result).toEqual(mockPassResult);
      expect(component.isPassed).toBe(true);
      expect(component.isFailed).toBe(false);
    });

    it('should show score from backend response', () => {
      expect(component.result.score).toBe(90);
    });

    it('should not compute score client-side', () => {
      // Verify component only displays backend data, no calculation
      expect(component.result.score).toBe(mockPassResult.score);
      expect(component.result.correctAnswers).toBe(mockPassResult.correctAnswers);
      expect(component.result.totalQuestions).toBe(mockPassResult.totalQuestions);
    });

    it('should render pass UI elements', () => {
      fixture.detectChanges();
      
      const passContainer = fixture.nativeElement.querySelector('.result-container.pass');
      expect(passContainer).toBeTruthy();
      
      const failContainer = fixture.nativeElement.querySelector('.result-container.fail');
      expect(failContainer).toBeFalsy();
    });

    it('should navigate back to lesson', () => {
      component.onBackToLesson();
      
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/courses', 'c1', 'lessons', 'l1']);
    });

    it('should navigate back to course', () => {
      component.onBackToCourse();
      
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/courses', 'c1']);
    });
  });

  describe('Fail Result', () => {
    beforeEach(() => {
      mockRouter.getCurrentNavigation.mockReturnValue({
        extras: { state: { result: mockFailResult } }
      });
      
      fixture = TestBed.createComponent(ExamResultComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('should display fail result from backend', () => {
      expect(component.result).toEqual(mockFailResult);
      expect(component.isPassed).toBe(false);
      expect(component.isFailed).toBe(true);
    });

    it('should show score from backend response', () => {
      expect(component.result.score).toBe(50);
    });

    it('should not compute score client-side', () => {
      // Verify component only displays backend data, no calculation
      expect(component.result.score).toBe(mockFailResult.score);
      expect(component.result.correctAnswers).toBe(mockFailResult.correctAnswers);
      expect(component.result.totalQuestions).toBe(mockFailResult.totalQuestions);
    });

    it('should render fail UI elements', () => {
      fixture.detectChanges();
      
      const failContainer = fixture.nativeElement.querySelector('.result-container.fail');
      expect(failContainer).toBeTruthy();
      
      const passContainer = fixture.nativeElement.querySelector('.result-container.pass');
      expect(passContainer).toBeFalsy();
    });

    it('should show retry option on failure', () => {
      fixture.detectChanges();
      
      const retryButton = fixture.nativeElement.querySelector('.primary-button');
      expect(retryButton).toBeTruthy();
      expect(retryButton.textContent).toContain('Retry');
    });

    it('should navigate to retry exam', () => {
      component.onRetry();
      
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/courses', 'c1', 'lessons', 'l1', 'exam', 'session']);
    });

    it('should display feedback from backend', () => {
      expect(component.result.feedback).toBe('Review the material and try again');
    });
  });

  describe('Error Handling', () => {
    beforeEach(() => {
      mockRouter.getCurrentNavigation.mockReturnValue(null);
      
      fixture = TestBed.createComponent(ExamResultComponent);
      component = fixture.componentInstance;
      fixture.detectChanges();
    });

    it('should set error when no result provided', () => {
      expect(component.error).toBe('No exam result found');
    });

    it('should set error when course ID not provided', () => {
      mockActivatedRoute.snapshot.paramMap.get = vi.fn((key: string) => {
        if (key === 'courseId') return null;
        if (key === 'lessonId') return 'l1';
        return null;
      });
      
      component.ngOnInit();
      
      expect(component.error).toBe('Course ID or Lesson ID not provided');
    });

    it('should set error when lesson ID not provided', () => {
      mockActivatedRoute.snapshot.paramMap.get = vi.fn((key: string) => {
        if (key === 'courseId') return 'c1';
        if (key === 'lessonId') return null;
        return null;
      });
      
      component.ngOnInit();
      
      expect(component.error).toBe('Course ID or Lesson ID not provided');
    });
  });

  describe('Empty State', () => {
    it('should show empty state when no result and no error', () => {
      mockRouter.getCurrentNavigation.mockReturnValue({
        extras: { state: { result: null } }
      });
      
      fixture = TestBed.createComponent(ExamResultComponent);
      component = fixture.componentInstance;
      component.result = null;
      component.error = null;
      component.courseId = 'c1';
      component.lessonId = 'l1';
      
      expect(component.isEmpty).toBe(true);
    });

    it('should not show empty state when error exists', () => {
      component.error = 'Error';
      
      expect(component.isEmpty).toBe(false);
    });

    it('should not show empty state when result exists', () => {
      component.result = mockPassResult;
      
      expect(component.isEmpty).toBe(false);
    });
  });
});
