import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ConfirmDialogComponent } from './confirm-dialog.component';
import { DebugElement } from '@angular/core';
import { By } from '@angular/platform-browser';
import { vi } from 'vitest';

describe('ConfirmDialogComponent', () => {
  let component: ConfirmDialogComponent;
  let fixture: ComponentFixture<ConfirmDialogComponent>;
  let compiled: DebugElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [ConfirmDialogComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(ConfirmDialogComponent);
    component = fixture.componentInstance;
    compiled = fixture.debugElement;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Dialog Content', () => {
    it('should display default title', () => {
      fixture.detectChanges();

      const titleElement = compiled.query(By.css('.dialog-title'));
      expect(titleElement.nativeElement.textContent).toBe('Confirm Action');
    });

    it('should display custom title', () => {
      component.title = 'Delete Course';
      fixture.detectChanges();

      const titleElement = compiled.query(By.css('.dialog-title'));
      expect(titleElement.nativeElement.textContent).toBe('Delete Course');
    });

    it('should display default message', () => {
      fixture.detectChanges();

      const messageElement = compiled.query(By.css('.dialog-message'));
      expect(messageElement.nativeElement.textContent).toBe('Are you sure you want to proceed?');
    });

    it('should display custom message', () => {
      component.message = 'This action cannot be undone.';
      fixture.detectChanges();

      const messageElement = compiled.query(By.css('.dialog-message'));
      expect(messageElement.nativeElement.textContent).toBe('This action cannot be undone.');
    });
  });

  describe('Button Labels', () => {
    it('should display default confirm label', () => {
      fixture.detectChanges();

      const confirmButton = compiled.query(By.css('.btn-confirm'));
      expect(confirmButton.nativeElement.textContent.trim()).toBe('Confirm');
    });

    it('should display custom confirm label', () => {
      component.confirmLabel = 'Delete';
      fixture.detectChanges();

      const confirmButton = compiled.query(By.css('.btn-confirm'));
      expect(confirmButton.nativeElement.textContent.trim()).toBe('Delete');
    });

    it('should display default cancel label', () => {
      fixture.detectChanges();

      const cancelButton = compiled.query(By.css('.btn-cancel'));
      expect(cancelButton.nativeElement.textContent.trim()).toBe('Cancel');
    });

    it('should display custom cancel label', () => {
      component.cancelLabel = 'Go Back';
      fixture.detectChanges();

      const cancelButton = compiled.query(By.css('.btn-cancel'));
      expect(cancelButton.nativeElement.textContent.trim()).toBe('Go Back');
    });
  });

  describe('Button Variants', () => {
    it('should apply danger variant by default', () => {
      fixture.detectChanges();

      const confirmButton = compiled.query(By.css('.btn-confirm'));
      expect(confirmButton.nativeElement.classList.contains('btn-danger')).toBe(true);
    });

    it('should apply primary variant when specified', () => {
      component.variant = 'primary';
      fixture.detectChanges();

      const confirmButton = compiled.query(By.css('.btn-confirm'));
      expect(confirmButton.nativeElement.classList.contains('btn-primary')).toBe(true);
      expect(confirmButton.nativeElement.classList.contains('btn-danger')).toBe(false);
    });

    it('should apply warning variant when specified', () => {
      component.variant = 'warning';
      fixture.detectChanges();

      const confirmButton = compiled.query(By.css('.btn-confirm'));
      expect(confirmButton.nativeElement.classList.contains('btn-warning')).toBe(true);
      expect(confirmButton.nativeElement.classList.contains('btn-danger')).toBe(false);
    });
  });

  describe('Confirm Action', () => {
    it('should emit confirm event when confirm button is clicked', () => {
      vi.spyOn(component.confirm, 'emit');
      fixture.detectChanges();

      const confirmButton = compiled.query(By.css('.btn-confirm'));
      confirmButton.nativeElement.click();

      expect(component.confirm.emit).toHaveBeenCalled();
    });

    it('should call onConfirm method when confirm button is clicked', () => {
      vi.spyOn(component, 'onConfirm');
      fixture.detectChanges();

      const confirmButton = compiled.query(By.css('.btn-confirm'));
      confirmButton.nativeElement.click();

      expect(component.onConfirm).toHaveBeenCalled();
    });
  });

  describe('Cancel Action', () => {
    it('should emit cancel event when cancel button is clicked', () => {
      vi.spyOn(component.cancel, 'emit');
      fixture.detectChanges();

      const cancelButton = compiled.query(By.css('.btn-cancel'));
      cancelButton.nativeElement.click();

      expect(component.cancel.emit).toHaveBeenCalled();
    });

    it('should call onCancel method when cancel button is clicked', () => {
      vi.spyOn(component, 'onCancel');
      fixture.detectChanges();

      const cancelButton = compiled.query(By.css('.btn-cancel'));
      cancelButton.nativeElement.click();

      expect(component.onCancel).toHaveBeenCalled();
    });
  });

  describe('Backdrop Interaction', () => {
    it('should emit cancel when backdrop is clicked', () => {
      vi.spyOn(component.cancel, 'emit');
      fixture.detectChanges();

      const backdrop = compiled.query(By.css('.dialog-backdrop'));
      const event = new MouseEvent('click');
      Object.defineProperty(event, 'target', { value: backdrop.nativeElement, enumerable: true });
      Object.defineProperty(event, 'currentTarget', { value: backdrop.nativeElement, enumerable: true });
      
      backdrop.nativeElement.dispatchEvent(event);

      expect(component.cancel.emit).toHaveBeenCalled();
    });

    it('should not emit cancel when clicking inside dialog', () => {
      vi.spyOn(component.cancel, 'emit');
      fixture.detectChanges();

      const backdrop = compiled.query(By.css('.dialog-backdrop'));
      const dialogContainer = compiled.query(By.css('.dialog-container'));
      
      const event = new MouseEvent('click');
      Object.defineProperty(event, 'target', { value: dialogContainer.nativeElement, enumerable: true });
      Object.defineProperty(event, 'currentTarget', { value: backdrop.nativeElement, enumerable: true });
      
      component.onBackdropClick(event);

      expect(component.cancel.emit).not.toHaveBeenCalled();
    });
  });

  describe('Keyboard Interaction', () => {
    it('should emit cancel when Escape key is pressed', () => {
      vi.spyOn(component.cancel, 'emit');
      fixture.detectChanges();

      const event = new KeyboardEvent('keydown', { key: 'Escape' });
      component.onKeyDown(event);

      expect(component.cancel.emit).toHaveBeenCalled();
    });

    it('should not emit cancel for other keys', () => {
      vi.spyOn(component.cancel, 'emit');
      fixture.detectChanges();

      const event = new KeyboardEvent('keydown', { key: 'Enter' });
      component.onKeyDown(event);

      expect(component.cancel.emit).not.toHaveBeenCalled();
    });
  });

  describe('Accessibility', () => {
    it('should have role="dialog" on backdrop', () => {
      fixture.detectChanges();

      const backdrop = compiled.query(By.css('.dialog-backdrop'));
      expect(backdrop.nativeElement.getAttribute('role')).toBe('dialog');
    });

    it('should have aria-modal="true" on backdrop', () => {
      fixture.detectChanges();

      const backdrop = compiled.query(By.css('.dialog-backdrop'));
      expect(backdrop.nativeElement.getAttribute('aria-modal')).toBe('true');
    });

    it('should have aria-labelledby pointing to title', () => {
      fixture.detectChanges();

      const backdrop = compiled.query(By.css('.dialog-backdrop'));
      expect(backdrop.nativeElement.getAttribute('aria-labelledby')).toBe('dialog-title');
    });

    it('should have aria-label on buttons', () => {
      fixture.detectChanges();

      const confirmButton = compiled.query(By.css('.btn-confirm'));
      const cancelButton = compiled.query(By.css('.btn-cancel'));
      
      expect(confirmButton.nativeElement.getAttribute('aria-label')).toBe('Confirm action');
      expect(cancelButton.nativeElement.getAttribute('aria-label')).toBe('Cancel action');
    });
  });
});
