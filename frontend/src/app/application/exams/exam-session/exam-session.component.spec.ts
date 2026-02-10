import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ExamSessionComponent } from './exam-session.component';
import { ApiService } from '../../../core/services/api.service';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

describe('ExamSessionComponent', () => {
  let component: ExamSessionComponent;
  let fixture: ComponentFixture<ExamSessionComponent>;
  let mockApiService: any;
  let mockRouter: any;
  let mockActivatedRoute: any;

  const mockExam = {
    id: 'exam1',
    title: 'Islamic Foundations Exam',
    description: 'Test your knowledge of Islamic foundations',
    questionCount: 3,
    passingScore: 70,
    timeLimit: 30,
    questions: [
      {
        id: 'q1',
        text: 'What are the five pillars of Islam?',
        choices: [
          { id: 'c1', text: 'Shahada, Salah, Zakat, Sawm, Hajj' },
          { id: 'c2', text: 'Faith, Prayer, Charity, Fasting' },
          { id: 'c3', text: 'Belief, Worship, Giving' }
        ]
      },
      {
        id: 'q2',
        text: 'What is the first pillar of Islam?',
        choices: [
          { id: 'c4', text: 'Salah (Prayer)' },
          { id: 'c5', text: 'Shahada (Declaration of Faith)' },
          { id: 'c6', text: 'Zakat (Charity)' }
        ]
      },
      {
        id: 'q3',
        text: 'How many times a day do Muslims pray?',
        choices: [
          { id: 'c7', text: 'Three times' },
          { id: 'c8', text: 'Five times' },
          { id: 'c9', text: 'Seven times' }
        ]
      }
    ]
  };

  beforeEach(async () => {
    mockApiService = {
      get: vi.fn(),
      post: vi.fn()
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
      imports: [ExamSessionComponent],
      providers: [
        { provide: ApiService, useValue: mockApiService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ExamSessionComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Initialization', () => {
    it('should load exam on init with course and lesson IDs from route', () => {
      mockApiService.get.mockReturnValue(of(mockExam));
      
      component.ngOnInit();
      
      expect(mockApiService.get).toHaveBeenCalledWith('api/courses/c1/lessons/l1/exam');
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
      mockApiService.get.mockReturnValue(of(mockExam));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.loading).toBe(false);
      expect(component.exam).toEqual(mockExam);
    });
  });

  describe('Data Loading', () => {
    it('should populate exam with API data', async () => {
      mockApiService.get.mockReturnValue(of(mockExam));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.exam).toEqual(mockExam);
    });

    it('should render questions in order received from backend', async () => {
      mockApiService.get.mockReturnValue(of(mockExam));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      expect(component.exam.questions[0].id).toBe('q1');
      expect(component.exam.questions[1].id).toBe('q2');
      expect(component.exam.questions[2].id).toBe('q3');
    });

    it('should not reorder questions', async () => {
      mockApiService.get.mockReturnValue(of(mockExam));
      
      component.ngOnInit();
      await fixture.whenStable();
      
      // Verify questions are in exact order from backend
      const questionIds = component.exam.questions.map((q: any) => q.id);
      expect(questionIds).toEqual(['q1', 'q2', 'q3']);
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
      
      expect(component.error).toBe('Failed to load exam');
    });

    it('should retry loading on retry', () => {
      mockApiService.get.mockReturnValue(of(mockExam));
      component.courseId = 'c1';
      component.lessonId = 'l1';
      
      component.onRetry();
      
      expect(mockApiService.get).toHaveBeenCalledWith('api/courses/c1/lessons/l1/exam');
    });
  });

  describe('Answer Selection', () => {
    beforeEach(async () => {
      mockApiService.get.mockReturnValue(of(mockExam));
      component.ngOnInit();
      await fixture.whenStable();
    });

    it('should track selected answer for a question', () => {
      component.onAnswerSelect('q1', 'c1');
      
      expect(component.selectedAnswers.get('q1')).toBe('c1');
    });

    it('should update selected answer when changed', () => {
      component.onAnswerSelect('q1', 'c1');
      component.onAnswerSelect('q1', 'c2');
      
      expect(component.selectedAnswers.get('q1')).toBe('c2');
    });

    it('should check if answer is selected', () => {
      component.onAnswerSelect('q1', 'c1');
      
      expect(component.isAnswerSelected('q1', 'c1')).toBe(true);
      expect(component.isAnswerSelected('q1', 'c2')).toBe(false);
    });

    it('should track multiple question answers independently', () => {
      component.onAnswerSelect('q1', 'c1');
      component.onAnswerSelect('q2', 'c5');
      
      expect(component.selectedAnswers.get('q1')).toBe('c1');
      expect(component.selectedAnswers.get('q2')).toBe('c5');
    });
  });

  describe('Submit Validation', () => {
    beforeEach(async () => {
      mockApiService.get.mockReturnValue(of(mockExam));
      component.ngOnInit();
      await fixture.whenStable();
    });

    it('should not allow submit when no answers selected', () => {
      expect(component.canSubmit()).toBe(false);
    });

    it('should not allow submit when only some questions answered', () => {
      component.onAnswerSelect('q1', 'c1');
      component.onAnswerSelect('q2', 'c5');
      
      expect(component.canSubmit()).toBe(false);
    });

    it('should allow submit when all questions answered', () => {
      component.onAnswerSelect('q1', 'c1');
      component.onAnswerSelect('q2', 'c5');
      component.onAnswerSelect('q3', 'c8');
      
      expect(component.canSubmit()).toBe(true);
    });
  });

  describe('Exam Submission', () => {
    beforeEach(async () => {
      mockApiService.get.mockReturnValue(of(mockExam));
      component.ngOnInit();
      await fixture.whenStable();
      
      // Answer all questions
      component.onAnswerSelect('q1', 'c1');
      component.onAnswerSelect('q2', 'c5');
      component.onAnswerSelect('q3', 'c8');
    });

    it('should submit answers to backend', () => {
      const mockResult = { passed: true, score: 100 };
      mockApiService.post.mockReturnValue(of(mockResult));
      
      component.onSubmit();
      
      expect(mockApiService.post).toHaveBeenCalledWith(
        'api/courses/c1/lessons/l1/exam/submit',
        {
          examId: 'exam1',
          answers: {
            q1: 'c1',
            q2: 'c5',
            q3: 'c8'
          }
        }
      );
    });

    it('should navigate to result page on successful submission', async () => {
      const mockResult = { passed: true, score: 100 };
      mockApiService.post.mockReturnValue(of(mockResult));
      
      component.onSubmit();
      await fixture.whenStable();
      
      expect(mockRouter.navigate).toHaveBeenCalledWith(
        ['/app/courses', 'c1', 'lessons', 'l1', 'exam', 'result'],
        { state: { result: mockResult } }
      );
    });

    it('should show error on submission failure', async () => {
      const error = new Error('Submission failed');
      mockApiService.post.mockReturnValue(throwError(() => error));
      
      component.onSubmit();
      await fixture.whenStable();
      
      expect(component.error).toBe('Submission failed');
      expect(component.submitting).toBe(false);
    });

    it('should not submit when already submitting', () => {
      mockApiService.post.mockReturnValue(of({ passed: true }));
      component.submitting = true;
      
      component.onSubmit();
      
      expect(mockApiService.post).not.toHaveBeenCalled();
    });

    it('should not submit when not all questions answered', () => {
      component.selectedAnswers.clear();
      component.onAnswerSelect('q1', 'c1');
      
      component.onSubmit();
      
      expect(mockApiService.post).not.toHaveBeenCalled();
    });
  });

  describe('Navigation', () => {
    beforeEach(async () => {
      mockApiService.get.mockReturnValue(of(mockExam));
      component.ngOnInit();
      await fixture.whenStable();
    });

    it('should navigate back to lesson on cancel', () => {
      component.onCancel();
      
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/app/courses', 'c1', 'lessons', 'l1']);
    });
  });

  describe('Empty State', () => {
    it('should show empty state when exam is null and not loading', () => {
      component.loading = false;
      component.error = null;
      component.exam = null;
      
      expect(component.isEmpty).toBe(true);
    });

    it('should not show empty state when loading', () => {
      component.loading = true;
      component.exam = null;
      
      expect(component.isEmpty).toBe(false);
    });

    it('should not show empty state when error exists', () => {
      component.error = 'Error';
      component.exam = null;
      
      expect(component.isEmpty).toBe(false);
    });
  });
});
