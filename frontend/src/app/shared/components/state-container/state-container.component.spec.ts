import { ComponentFixture, TestBed } from '@angular/core/testing';
import { StateContainerComponent } from './state-container.component';
import { DebugElement } from '@angular/core';
import { By } from '@angular/platform-browser';
import { vi } from 'vitest';

describe('StateContainerComponent', () => {
  let component: StateContainerComponent;
  let fixture: ComponentFixture<StateContainerComponent>;
  let compiled: DebugElement;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [StateContainerComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(StateContainerComponent);
    component = fixture.componentInstance;
    compiled = fixture.debugElement;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('State Priority', () => {
    it('should show loading state when loading is true', () => {
      component.loading = true;
      component.error = 'Some error';
      component.empty = true;
      fixture.detectChanges();

      expect(component.currentState).toBe('loading');
      const loadingElement = compiled.query(By.css('.state-loading'));
      expect(loadingElement).toBeTruthy();
      expect(loadingElement.nativeElement.textContent).toContain('Loading...');
    });

    it('should show error state when error is set and not loading', () => {
      component.loading = false;
      component.error = 'An error occurred';
      component.empty = true;
      fixture.detectChanges();

      expect(component.currentState).toBe('error');
      const errorElement = compiled.query(By.css('.state-error'));
      expect(errorElement).toBeTruthy();
      expect(errorElement.nativeElement.textContent).toContain('An error occurred');
    });

    it('should show empty state when empty is true and no loading/error', () => {
      component.loading = false;
      component.error = null;
      component.empty = true;
      fixture.detectChanges();

      expect(component.currentState).toBe('empty');
      const emptyElement = compiled.query(By.css('.state-empty'));
      expect(emptyElement).toBeTruthy();
      expect(emptyElement.nativeElement.textContent).toContain('No data available');
    });

    it('should show data state when no loading/error/empty', () => {
      component.loading = false;
      component.error = null;
      component.empty = false;
      fixture.detectChanges();

      expect(component.currentState).toBe('data');
      const dataElement = compiled.query(By.css('.state-data'));
      expect(dataElement).toBeTruthy();
    });
  });

  describe('Retry Functionality', () => {
    it('should emit retry event when retry button is clicked', () => {
      vi.spyOn(component.retry, 'emit');
      component.error = 'Network error';
      fixture.detectChanges();

      const retryButton = compiled.query(By.css('.retry-button'));
      expect(retryButton).toBeTruthy();
      
      retryButton.nativeElement.click();
      expect(component.retry.emit).toHaveBeenCalled();
    });

    it('should not show retry button when not in error state', () => {
      component.loading = true;
      fixture.detectChanges();

      const retryButton = compiled.query(By.css('.retry-button'));
      expect(retryButton).toBeFalsy();
    });
  });

  describe('Content Projection', () => {
    it('should project content in data state', () => {
      const testContent = '<div class="test-content">Test Content</div>';
      const hostElement = fixture.nativeElement;
      hostElement.innerHTML = testContent;
      
      component.loading = false;
      component.error = null;
      component.empty = false;
      fixture.detectChanges();

      const dataElement = compiled.query(By.css('.state-data'));
      expect(dataElement).toBeTruthy();
    });
  });

  describe('State Transitions', () => {
    it('should have correct initial state', () => {
      component.loading = false;
      component.error = null;
      component.empty = false;
      fixture.detectChanges();
      
      expect(component.currentState).toBe('data');
    });
  });
});
