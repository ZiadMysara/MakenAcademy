import { ComponentFixture, TestBed } from '@angular/core/testing';
import { FormShellComponent } from './form-shell.component';
import { DebugElement } from '@angular/core';
import { By } from '@angular/platform-browser';
import { vi } from 'vitest';

describe('FormShellComponent', () => {
  let component: FormShellComponent;
  let fixture: ComponentFixture<FormShellComponent>;
  let compiled: DebugElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FormShellComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(FormShellComponent);
    component = fixture.componentInstance;
    compiled = fixture.debugElement;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Form Title', () => {
    it('should display title when provided', () => {
      component.title = 'Create Course';
      fixture.detectChanges();

      const titleElement = compiled.query(By.css('.form-title'));
      expect(titleElement).toBeTruthy();
      expect(titleElement.nativeElement.textContent).toBe('Create Course');
    });

    it('should not display header when title is empty', () => {
      component.title = '';
      fixture.detectChanges();

      const headerElement = compiled.query(By.css('.form-header'));
      expect(headerElement).toBeFalsy();
    });
  });

  describe('Success Message', () => {
    it('should display success message when provided', () => {
      component.successMessage = 'Course created successfully';
      fixture.detectChanges();

      const successAlert = compiled.query(By.css('.alert-success'));
      expect(successAlert).toBeTruthy();
      expect(successAlert.nativeElement.textContent).toContain('Course created successfully');
    });

    it('should not display success alert when message is null', () => {
      component.successMessage = null;
      fixture.detectChanges();

      const successAlert = compiled.query(By.css('.alert-success'));
      expect(successAlert).toBeFalsy();
    });
  });

  describe('Error Handling', () => {
    it('should display error alert when errors array is provided', () => {
      component.errors = ['Error 1', 'Error 2'];
      fixture.detectChanges();

      expect(component.hasErrors).toBe(true);
      const errorAlert = compiled.query(By.css('.alert-error'));
      expect(errorAlert).toBeTruthy();
    });

    it('should display error alert when errors object is provided', () => {
      component.errors = {
        name: ['Name is required'],
        email: ['Email is invalid']
      };
      fixture.detectChanges();

      expect(component.hasErrors).toBe(true);
      const errorAlert = compiled.query(By.css('.alert-error'));
      expect(errorAlert).toBeTruthy();
    });

    it('should flatten object errors into array', () => {
      component.errors = {
        name: ['Name is required', 'Name too short'],
        email: ['Email is invalid']
      };

      const messages = component.errorMessages;
      expect(messages.length).toBe(3);
      expect(messages).toContain('name: Name is required');
      expect(messages).toContain('name: Name too short');
      expect(messages).toContain('email: Email is invalid');
    });

    it('should display all error messages in list', () => {
      component.errors = ['Error 1', 'Error 2', 'Error 3'];
      fixture.detectChanges();

      const errorItems = compiled.queryAll(By.css('.error-list li'));
      expect(errorItems.length).toBe(3);
      expect(errorItems[0].nativeElement.textContent).toBe('Error 1');
      expect(errorItems[1].nativeElement.textContent).toBe('Error 2');
      expect(errorItems[2].nativeElement.textContent).toBe('Error 3');
    });

    it('should not display error alert when errors is null', () => {
      component.errors = null;
      fixture.detectChanges();

      expect(component.hasErrors).toBe(false);
      const errorAlert = compiled.query(By.css('.alert-error'));
      expect(errorAlert).toBeFalsy();
    });

    it('should not display error alert when errors is empty array', () => {
      component.errors = [];
      fixture.detectChanges();

      expect(component.hasErrors).toBe(false);
      const errorAlert = compiled.query(By.css('.alert-error'));
      expect(errorAlert).toBeFalsy();
    });
  });

  describe('Form Submission', () => {
    it('should emit formSubmit event when submit button is clicked', () => {
      vi.spyOn(component.formSubmit, 'emit');
      fixture.detectChanges();

      const submitButton = compiled.query(By.css('.btn-submit'));
      submitButton.nativeElement.click();

      expect(component.formSubmit.emit).toHaveBeenCalled();
    });

    it('should not emit formSubmit when submitting is true', () => {
      vi.spyOn(component.formSubmit, 'emit');
      component.submitting = true;
      fixture.detectChanges();

      component.onSubmit();
      expect(component.formSubmit.emit).not.toHaveBeenCalled();
    });

    it('should disable submit button when submitting', () => {
      component.submitting = true;
      fixture.detectChanges();

      const submitButton = compiled.query(By.css('.btn-submit'));
      expect(submitButton.nativeElement.disabled).toBe(true);
    });

    it('should show submitting state on button', () => {
      component.submitting = true;
      fixture.detectChanges();

      const submitButton = compiled.query(By.css('.btn-submit'));
      expect(submitButton.nativeElement.textContent).toContain('Submitting...');
    });

    it('should use custom submit label', () => {
      component.submitLabel = 'Save Changes';
      fixture.detectChanges();

      const submitButton = compiled.query(By.css('.btn-submit'));
      expect(submitButton.nativeElement.textContent).toContain('Save Changes');
    });
  });

  describe('Form Cancellation', () => {
    it('should emit formCancel event when cancel button is clicked', () => {
      vi.spyOn(component.formCancel, 'emit');
      fixture.detectChanges();

      const cancelButton = compiled.query(By.css('.btn-cancel'));
      cancelButton.nativeElement.click();

      expect(component.formCancel.emit).toHaveBeenCalled();
    });

    it('should disable cancel button when submitting', () => {
      component.submitting = true;
      fixture.detectChanges();

      const cancelButton = compiled.query(By.css('.btn-cancel'));
      expect(cancelButton.nativeElement.disabled).toBe(true);
    });

    it('should hide cancel button when showCancel is false', () => {
      component.showCancel = false;
      fixture.detectChanges();

      const cancelButton = compiled.query(By.css('.btn-cancel'));
      expect(cancelButton).toBeFalsy();
    });

    it('should use custom cancel label', () => {
      component.cancelLabel = 'Go Back';
      fixture.detectChanges();

      const cancelButton = compiled.query(By.css('.btn-cancel'));
      expect(cancelButton.nativeElement.textContent.trim()).toBe('Go Back');
    });
  });

  describe('Content Projection', () => {
    it('should project form content', () => {
      const hostElement = fixture.nativeElement;
      hostElement.innerHTML = '<div class="test-form">Form Fields</div>';
      fixture.detectChanges();

      const formContent = compiled.query(By.css('.form-content'));
      expect(formContent).toBeTruthy();
    });
  });

  describe('Form States', () => {
    it('should handle pristine state (no errors, not submitting)', () => {
      component.errors = null;
      component.submitting = false;
      fixture.detectChanges();

      const errorAlert = compiled.query(By.css('.alert-error'));
      const submitButton = compiled.query(By.css('.btn-submit'));
      
      expect(errorAlert).toBeFalsy();
      expect(submitButton.nativeElement.disabled).toBe(false);
    });

    it('should handle dirty state with errors', () => {
      component.errors = ['Validation error'];
      component.submitting = false;
      fixture.detectChanges();

      const errorAlert = compiled.query(By.css('.alert-error'));
      expect(errorAlert).toBeTruthy();
    });

    it('should handle submitting state', () => {
      component.submitting = true;
      fixture.detectChanges();

      const submitButton = compiled.query(By.css('.btn-submit'));
      const cancelButton = compiled.query(By.css('.btn-cancel'));
      
      expect(submitButton.nativeElement.disabled).toBe(true);
      expect(cancelButton.nativeElement.disabled).toBe(true);
    });
  });
});
