import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * FormShellComponent
 * 
 * Generic form wrapper that manages local form state (pristine/dirty/submitting/error).
 * Displays backend validation errors and emits submit/cancel events.
 * Does NOT validate business rules - validation is backend-driven.
 * 
 * Constitution Compliance:
 * - [FEAT-009] Does NOT duplicate backend validation logic
 * - [FEAT-021] Manages only local UI state
 * - [FEAT-022] Does NOT persist business state on client
 * - [FEAT-018] Data via inputs, events via outputs
 * - Mobile-first responsive (Constitution §12.I)
 * 
 * Usage:
 * <app-form-shell 
 *   [title]="'Create Course'"
 *   [submitting]="isSubmitting"
 *   [errors]="backendErrors"
 *   (formSubmit)="onSubmit()"
 *   (formCancel)="onCancel()">
 *   <form>Your form fields here</form>
 * </app-form-shell>
 */
@Component({
  selector: 'app-form-shell',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './form-shell.component.html',
  styleUrls: ['./form-shell.component.css']
})
export class FormShellComponent {
  /**
   * Form title
   */
  @Input() title: string = '';

  /**
   * Indicates if form is currently submitting
   */
  @Input() submitting: boolean = false;

  /**
   * Backend validation errors (key-value pairs or array of strings)
   */
  @Input() errors: Record<string, string[]> | string[] | null = null;

  /**
   * Success message to display after successful submission
   */
  @Input() successMessage: string | null = null;

  /**
   * Show cancel button
   */
  @Input() showCancel: boolean = true;

  /**
   * Submit button label
   */
  @Input() submitLabel: string = 'Submit';

  /**
   * Cancel button label
   */
  @Input() cancelLabel: string = 'Cancel';

  /**
   * Emitted when form is submitted
   */
  @Output() formSubmit = new EventEmitter<void>();

  /**
   * Emitted when cancel button is clicked
   */
  @Output() formCancel = new EventEmitter<void>();

  /**
   * Handles form submission
   */
  onSubmit(): void {
    if (!this.submitting) {
      this.formSubmit.emit();
    }
  }

  /**
   * Handles cancel button click
   */
  onCancel(): void {
    this.formCancel.emit();
  }

  /**
   * Checks if there are any errors
   */
  get hasErrors(): boolean {
    if (!this.errors) return false;
    if (Array.isArray(this.errors)) return this.errors.length > 0;
    return Object.keys(this.errors).length > 0;
  }

  /**
   * Gets error messages as array
   */
  get errorMessages(): string[] {
    if (!this.errors) return [];
    
    if (Array.isArray(this.errors)) {
      return this.errors;
    }
    
    // Flatten object errors into array
    const messages: string[] = [];
    for (const [field, fieldErrors] of Object.entries(this.errors)) {
      if (Array.isArray(fieldErrors)) {
        fieldErrors.forEach(error => {
          messages.push(`${field}: ${error}`);
        });
      } else {
        messages.push(`${field}: ${fieldErrors}`);
      }
    }
    return messages;
  }
}
