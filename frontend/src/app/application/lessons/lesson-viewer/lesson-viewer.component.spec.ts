import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LessonViewerComponent } from './lesson-viewer.component';
import { ApiService } from '../../../core/services/api.service';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

describe('LessonViewerComponent', () => {
  let component: LessonViewerComponent;
  let fixture: ComponentFixture<LessonViewerComponent>;
  let mockApiService: any;
  let mockRouter: any;
  let mockActivatedRoute: any;

  const mockVideoLesson = {
    id: 'l1',
    title: 'Introduction to Islam',
    description: 'Learn the basics of Islam',
    contentType: 'video',
    contentUrl: 'https://example.com/video.mp4',
    thumbnailUrl: 'https://example.com/thumb.jpg',
    isCompleted: false,
    isLocked: false,
    hasExam: true,
    duration: '15 min',
    order: 1
  };

  const mockPdfLesson = {
    id: 'l2',
    title: 'Islamic History',
    description: 'Study Islamic history',
    contentType: 'pdf',
    contentUrl: 'https://example.com/document.pdf',
    isCompleted: true,
    isLocked: false,
    hasExam: false,
    duration: '20 min',
    order: 2
  };

  const mockLockedLesson = {
    id: 'l3',
    title: 'Advanced Topics',
    description: 'Advanced Islamic studies',
    contentType: 'video',
    contentUrl: 'https://example.com/video2.mp4',
    isCompleted: false,
    isLocked: true,
    hasExam: false,
    order: 3
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
          get: vi.fn((key: string) => {
            if (key === 'courseId') return 'c1';
            if (key === 'lessonId') return 'l1';
            return null;
          })
        }
      }
    };

    await TestBed.configureTestingModule({
      imports: [LessonViewerComponent],
      providers: [
        { provide: ApiService, useValue: mockApiService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(LessonViewerComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Initialization', () => {
    it('should load lesson on init with course and lesson IDs from route', () => {
      mockApiService.get.mockReturnValue(of(mockVideoLesson));
      
      component.ngOnInit();
      
      expect(mockApiService.get).toHaveBeenCalledWith('api/courses/c1/lessons/l1');
    });

    it('should set error when course ID is not provided', () => {
      mockActivatedRoute.snapshot.paramMap.get = vi.fn((key: string) => {
        if (key === 'courseId') return null;
        if (key === 'lessonId') return 'l1';
        return null;
      });
      
      component.ngOnInit();
      
      expect(component.error).toBe('Course ID or Lesson ID not provided');
    });

    it('should set error when lesson ID is not provided', () => {
      mockActivatedRoute.snapshot.paramMap.get = vi.fn((key: string) => {
        if (key === 'courseId') return 'c1';
        if (key === 'lessonId') return null;
        return null;
      });
      
      component.ngOnInit();
      
      expect(component.error).toBe('Course ID or Lesson ID not provided');
    });
  });

  describe('Loading State', () => {
    it('should hide loading state after data loads', async () => {
      mockApiService.get.mockReturnValue(of(mockVideoLesson));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.loading).toBe(false);
      expect(component.lesson).toEqual(mockVideoLesson);
    });
  });

  describe('Data Loading', () => {
    it('should populate lesson with API data', async () => {
      mockApiService.get.mockReturnValue(of(mockVideoLesson));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.lesson).toEqual(mockVideoLesson);
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
      
      expect(component.error).toBe('Failed to load lesson');
    });

    it('should retry loading on retry', () => {
      mockApiService.get.mockReturnValue(of(mockVideoLesson));
      component.courseId = 'c1';
      component.lessonId = 'l1';
      
      component.onRetry();
      
      expect(mockApiService.get).toHaveBeenCalledWith('api/courses/c1/lessons/l1');
    });
  });

  describe('Video Rendering', () => {
    it('should identify video content type correctly', async () => {
      mockApiService.get.mockReturnValue(of(mockVideoLesson));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.isVideo).toBe(true);
      expect(component.isPdf).toBe(false);
    });

    it('should render video when type is video', async () => {
      mockApiService.get.mockReturnValue(of(mockVideoLesson));
      
      component.ngOnInit();
      await fixture.whenStable();
      fixture.detectChanges();
      
      const videoElement = fixture.nativeElement.querySelector('.video-player');
      expect(videoElement).toBeTruthy();
      expect(videoElement.src).toContain('video.mp4');
    });
  });

  describe('PDF Rendering', () => {
    it('should identify PDF content type correctly', async () => {
      mockApiService.get.mockReturnValue(of(mockPdfLesson));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.isPdf).toBe(true);
      expect(component.isVideo).toBe(false);
    });

    it('should render PDF when type is pdf', async () => {
      mockApiService.get.mockReturnValue(of(mockPdfLesson));
      
      component.ngOnInit();
      await fixture.whenStable();
      fixture.detectChanges();
      
      const pdfElement = fixture.nativeElement.querySelector('.pdf-viewer');
      expect(pdfElement).toBeTruthy();
      expect(pdfElement.src).toContain('document.pdf');
    });
  });

  describe('Completion Indicator', () => {
    it('should show completion badge when lesson is completed', async () => {
      mockApiService.get.mockReturnValue(of(mockPdfLesson));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.isCompleted).toBe(true);
    });

    it('should not show completion badge when lesson is not completed', async () => {
      mockApiService.get.mockReturnValue(of(mockVideoLesson));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.isCompleted).toBe(false);
    });

    it('should render completion badge in DOM when completed', async () => {
      mockApiService.get.mockReturnValue(of(mockPdfLesson));
      
      component.ngOnInit();
      await fixture.whenStable();
      fixture.detectChanges();
      
      const badge = fixture.nativeElement.querySelector('.completion-badge');
      expect(badge).toBeTruthy();
      expect(badge.textContent).toContain('Completed');
    });
  });

  describe('Exam Button Visibility', () => {
    it('should show exam button when backend indicates exam available', async () => {
      mockApiService.get.mockReturnValue(of(mockVideoLesson));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.hasExam).toBe(true);
    });

    it('should not show exam button when backend indicates no exam', async () => {
      mockApiService.get.mockReturnValue(of(mockPdfLesson));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.hasExam).toBe(false);
    });

    it('should render exam button in DOM when exam available', async () => {
      mockApiService.get.mockReturnValue(of(mockVideoLesson));
      
      component.ngOnInit();
      await fixture.whenStable();
      fixture.detectChanges();
      
      const examButton = fixture.nativeElement.querySelector('.exam-button');
      expect(examButton).toBeTruthy();
      expect(examButton.textContent).toContain('Take Exam');
    });

    it('should not render exam button when no exam available', async () => {
      mockApiService.get.mockReturnValue(of(mockPdfLesson));
      
      component.ngOnInit();
      await fixture.whenStable();
      fixture.detectChanges();
      
      const examButton = fixture.nativeElement.querySelector('.exam-button');
      expect(examButton).toBeFalsy();
    });
  });

  describe('Locked State', () => {
    it('should identify locked lesson correctly', async () => {
      mockApiService.get.mockReturnValue(of(mockLockedLesson));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.isLocked).toBe(true);
    });

    it('should render locked state when lesson is locked', async () => {
      mockApiService.get.mockReturnValue(of(mockLockedLesson));
      
      component.ngOnInit();
      await fixture.whenStable();
      fixture.detectChanges();
      
      const lockedState = fixture.nativeElement.querySelector('.locked-state');
      expect(lockedState).toBeTruthy();
      expect(lockedState.textContent).toContain('This lesson is locked');
    });

    it('should not render content when lesson is locked', async () => {
      mockApiService.get.mockReturnValue(of(mockLockedLesson));
      
      component.ngOnInit();
      await fixture.whenStable();
      fixture.detectChanges();
      
      const contentArea = fixture.nativeElement.querySelector('.content-area');
      expect(contentArea).toBeFalsy();
    });
  });

  describe('Navigation', () => {
    beforeEach(async () => {
      mockApiService.get.mockReturnValue(of(mockVideoLesson));
      component.ngOnInit();
      await fixture.whenStable();
    });

    it('should navigate back to course detail', () => {
      component.onBackClick();
      
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/courses', 'c1']);
    });

    it('should navigate to exam when exam button clicked', () => {
      component.onStartExam();
      
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/courses', 'c1', 'lessons', 'l1', 'exam']);
    });
  });

  describe('Empty State', () => {
    it('should show empty state when lesson is null and not loading', () => {
      component.loading = false;
      component.error = null;
      component.lesson = null;
      
      expect(component.isEmpty).toBe(true);
    });

    it('should not show empty state when loading', () => {
      component.loading = true;
      component.lesson = null;
      
      expect(component.isEmpty).toBe(false);
    });

    it('should not show empty state when error exists', () => {
      component.error = 'Error';
      component.lesson = null;
      
      expect(component.isEmpty).toBe(false);
    });
  });
});
