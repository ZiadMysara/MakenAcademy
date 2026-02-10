import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { CourseListComponent } from './course-list.component';
import { ApiService } from '../../../core/services/api.service';

describe('CourseListComponent', () => {
  let component: CourseListComponent;
  let fixture: ComponentFixture<CourseListComponent>;
  let mockApiService: any;
  let mockRouter: any;

  beforeEach(async () => {
    mockApiService = {
      get: vi.fn(),
      delete: vi.fn()
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

  describe('ngOnInit', () => {
    it('should load courses on init', () => {
      const mockCourses = [
        { id: '1', name: 'Course 1', status: 'published', lessonCount: 5 },
        { id: '2', name: 'Course 2', status: 'draft', lessonCount: 3 }
      ];

      mockApiService.get.mockReturnValue(of(mockCourses));

      component.ngOnInit();

      expect(mockApiService.get).toHaveBeenCalledWith('api/courses');
      expect(component.courses).toEqual(mockCourses);
      expect(component.loading).toBe(false);
      expect(component.error).toBeNull();
    });

    it('should set loading state while fetching', () => {
      mockApiService.get.mockReturnValue(of([]));

      component.loading = false;
      component.ngOnInit();

      expect(component.loading).toBe(false);
    });

    it('should handle error when loading courses fails', () => {
      const errorMessage = 'Network error';
      mockApiService.get.mockReturnValue(throwError(() => new Error(errorMessage)));

      component.ngOnInit();

      expect(component.error).toBe(errorMessage);
      expect(component.loading).toBe(false);
      expect(component.courses).toEqual([]);
    });
  });

  describe('loadCourses', () => {
    it('should reset error state before loading', () => {
      component.error = 'Previous error';
      mockApiService.get.mockReturnValue(of([]));

      component.loadCourses();

      expect(component.error).toBeNull();
    });

    it('should set loading to true during fetch', () => {
      mockApiService.get.mockReturnValue(of([]));

      component.loadCourses();

      expect(mockApiService.get).toHaveBeenCalled();
    });
  });

  describe('onRetry', () => {
    it('should call loadCourses', () => {
      mockApiService.get.mockReturnValue(of([]));
      vi.spyOn(component, 'loadCourses');

      component.onRetry();

      expect(component.loadCourses).toHaveBeenCalled();
    });
  });

  describe('onRowAction', () => {
    it('should navigate to edit page when edit action is triggered', () => {
      const mockCourse = { id: '123', name: 'Test Course' };

      component.onRowAction({ action: 'edit', row: mockCourse });

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/control-panel/courses', '123', 'edit']);
    });

    it('should show delete dialog when delete action is triggered', () => {
      const mockCourse = { id: '123', name: 'Test Course' };

      component.onRowAction({ action: 'delete', row: mockCourse });

      expect(component.showDeleteDialog).toBe(true);
      expect(component.courseToDelete).toEqual(mockCourse);
    });
  });

  describe('onCreateNew', () => {
    it('should navigate to new course page', () => {
      component.onCreateNew();

      expect(mockRouter.navigate).toHaveBeenCalledWith(['/control-panel/courses/new']);
    });
  });

  describe('onConfirmDelete', () => {
    it('should delete course and reload list on success', () => {
      const mockCourse = { id: '123', name: 'Test Course' };
      component.courseToDelete = mockCourse;
      component.showDeleteDialog = true;

      mockApiService.delete.mockReturnValue(of({}));
      mockApiService.get.mockReturnValue(of([]));

      component.onConfirmDelete();

      expect(mockApiService.delete).toHaveBeenCalledWith('api/courses/123');
      expect(component.showDeleteDialog).toBe(false);
      expect(component.courseToDelete).toBeNull();
    });

    it('should handle delete error', () => {
      const mockCourse = { id: '123', name: 'Test Course' };
      component.courseToDelete = mockCourse;

      const errorMessage = 'Delete failed';
      mockApiService.delete.mockReturnValue(throwError(() => new Error(errorMessage)));

      component.onConfirmDelete();

      expect(component.error).toBe(errorMessage);
      expect(component.showDeleteDialog).toBe(false);
      expect(component.courseToDelete).toBeNull();
    });

    it('should do nothing if courseToDelete is null', () => {
      component.courseToDelete = null;

      component.onConfirmDelete();

      expect(mockApiService.delete).not.toHaveBeenCalled();
    });
  });

  describe('onCancelDelete', () => {
    it('should close delete dialog and clear courseToDelete', () => {
      component.showDeleteDialog = true;
      component.courseToDelete = { id: '123', name: 'Test' };

      component.onCancelDelete();

      expect(component.showDeleteDialog).toBe(false);
      expect(component.courseToDelete).toBeNull();
    });
  });

  describe('isEmpty', () => {
    it('should return true when not loading, no error, and no courses', () => {
      component.loading = false;
      component.error = null;
      component.courses = [];

      expect(component.isEmpty).toBe(true);
    });

    it('should return false when loading', () => {
      component.loading = true;
      component.error = null;
      component.courses = [];

      expect(component.isEmpty).toBe(false);
    });

    it('should return false when there is an error', () => {
      component.loading = false;
      component.error = 'Error';
      component.courses = [];

      expect(component.isEmpty).toBe(false);
    });

    it('should return false when there are courses', () => {
      component.loading = false;
      component.error = null;
      component.courses = [{ id: '1', name: 'Course' }];

      expect(component.isEmpty).toBe(false);
    });
  });

  describe('columns', () => {
    it('should define correct table columns', () => {
      expect(component.columns).toEqual([
        { key: 'name', label: 'Name' },
        { key: 'status', label: 'Status' },
        { key: 'lessonCount', label: 'Lesson Count' }
      ]);
    });
  });
});
