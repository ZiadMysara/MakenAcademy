import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { CourseFormComponent } from './course-form.component';
import { ApiService } from '../../../core/services/api.service';

describe('CourseFormComponent', () => {
  let component: CourseFormComponent;
  let fixture: ComponentFixture<CourseFormComponent>;
  let mockApiService: any;
  let mockRouter: any;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    mockApiService = {
      get: vi.fn(),
      post: vi.fn(),
      put: vi.fn()
    };

    mockRouter = {
      navigate: vi.fn()
    };

    mockActivatedRoute = {
      snapshot: {
        paramMap: {
          get: vi.fn()
        }
      }
    };

    await TestBed.configureTestingModule({
      imports: [CourseFormComponent],
      providers: [
        { provide: ApiService, useValue: mockApiService },
        { provide: Router, useValue: mockRouter },
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(CourseFormComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('ngOnInit - Create Mode', () => {
    beforeEach(() => {
      mockActivatedRoute.snapshot.paramMap.get.mockReturnValue(null);
    });

    it('should initialize in create mode when no id param', () => {
      component.ngOnInit();

      expect(component.isEditMode).toBe(false);
      expect(component.courseId).toBeNull();
      expect(mockApiService.get).not.toHaveBeenCalled();
    });

    it('should initialize with default course values', () => {
      component.ngOnInit();

      expect(component.course).toEqual({
        name: '',
        description: '',
        status: 'draft'
      });
    });
  });

  describe('ngOnInit - Edit Mode', () => {
    beforeEach(() => {
      mockActivatedRoute.snapshot.paramMap.get.mockReturnValue('123');
    });

    it('should initialize in edit mode when id param exists', () => {
      mockApiService.get.mockReturnValue(of({
        name: 'Test Course',
        description: 'Test Description',
        status: 'published'
      }));

      component.ngOnInit();

      expect(component.isEditMode).toBe(true);
      expect(component.courseId).toBe('123');
      expect(mockApiService.get).toHaveBeenCalledWith('api/courses/123');
    });

    it('should load course data in edit mode', () => {
      const mockCourse = {
        name: 'Test Course',
        description: 'Test Description',
        status: 'published'
      };

      mockApiService.get.mockReturnValue(of(mockCourse));

      component.ngOnInit();

      expect(component.course).toEqual(mockCourse);
    });

    it('should handle missing fields in loaded course', () => {
      mockApiService.get.mockReturnValue(of({ name: 'Test' }));

      component.ngOnInit();

      expect(component.course).toEqual({
        name: 'Test',
        description: '',
        status: 'draft'
      });
    });

    it('should handle error when loading course fails', () => {
      const errorMessage = 'Failed to load';
      mockApiService.get.mockReturnValue(throwError(() => new Error(errorMessage)));

      component.ngOnInit();

      expect(component.backendErrors).toEqual({ general: errorMessage });
    });
  });

  describe('onFieldChange', () => {
    it('should set pristine to false', () => {
      component.pristine = true;

      component.onFieldChange();

      expect(component.pristine).toBe(false);
    });
  });

  describe('onSubmit - Create Mode', () => {
    beforeEach(() => {
      component.isEditMode = false;
      component.courseId = null;
    });

    it('should call post API in create mode', () => {
      mockApiService.post.mockReturnValue(of({}));

      component.course = {
        name: 'New Course',
        description: 'Description',
        status: 'draft'
      };

      component.onSubmit();

      expect(mockApiService.post).toHaveBeenCalledWith('api/courses', component.course);
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/control-panel/courses']);
    });

    it('should set submitting state during submission', () => {
      mockApiService.post.mockReturnValue(of({}));

      component.submitting = false;
      component.onSubmit();

      expect(component.submitting).toBe(true);
    });

    it('should handle backend validation errors', () => {
      const backendError = {
        error: {
          name: ['Name is required'],
          description: ['Description too short']
        }
      };

      mockApiService.post.mockReturnValue(throwError(() => backendError));

      component.onSubmit();

      expect(component.backendErrors).toEqual(backendError.error);
      expect(component.submitting).toBe(false);
      expect(mockRouter.navigate).not.toHaveBeenCalled();
    });

    it('should handle generic error without error object', () => {
      const errorMessage = 'Network error';
      mockApiService.post.mockReturnValue(throwError(() => new Error(errorMessage)));

      component.onSubmit();

      expect(component.backendErrors).toEqual({ general: errorMessage });
      expect(component.submitting).toBe(false);
    });

    it('should not submit if already submitting', () => {
      component.submitting = true;

      component.onSubmit();

      expect(mockApiService.post).not.toHaveBeenCalled();
      expect(mockApiService.put).not.toHaveBeenCalled();
    });
  });

  describe('onSubmit - Edit Mode', () => {
    beforeEach(() => {
      component.isEditMode = true;
      component.courseId = '123';
    });

    it('should call put API in edit mode', () => {
      mockApiService.put.mockReturnValue(of({}));

      component.course = {
        name: 'Updated Course',
        description: 'Updated Description',
        status: 'published'
      };

      component.onSubmit();

      expect(mockApiService.put).toHaveBeenCalledWith('api/courses/123', component.course);
      expect(mockRouter.navigate).toHaveBeenCalledWith(['/control-panel/courses']);
    });

    it('should handle update errors', () => {
      const backendError = {
        error: { name: ['Invalid name'] }
      };

      mockApiService.put.mockReturnValue(throwError(() => backendError));

      component.onSubmit();

      expect(component.backendErrors).toEqual(backendError.error);
      expect(component.submitting).toBe(false);
    });
  });

  describe('onCancel', () => {
    it('should navigate back to course list', () => {
      component.onCancel();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/control-panel/courses']);
    });
  });

  describe('isDirty', () => {
    it('should return true when form is not pristine', () => {
      component.pristine = false;

      expect(component.isDirty).toBe(true);
    });

    it('should return false when form is pristine', () => {
      component.pristine = true;

      expect(component.isDirty).toBe(false);
    });
  });

  describe('initial state', () => {
    it('should have pristine set to true initially', () => {
      expect(component.pristine).toBe(true);
    });

    it('should have submitting set to false initially', () => {
      expect(component.submitting).toBe(false);
    });

    it('should have backendErrors set to null initially', () => {
      expect(component.backendErrors).toBeNull();
    });
  });
});
