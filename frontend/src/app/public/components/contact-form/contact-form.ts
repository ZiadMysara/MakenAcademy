import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { PublicApi } from '../../services/public-api';

@Component({
  selector: 'app-contact-form',
  imports: [CommonModule, ReactiveFormsModule],
  templateUrl: './contact-form.html',
  styleUrl: './contact-form.css',
})
export class ContactForm {
  private readonly publicApi = inject(PublicApi);
  private readonly fb = inject(FormBuilder);

  contactForm: FormGroup;
  isSubmitting = false;
  submitSuccess = false;
  submitError: string | null = null;

  constructor() {
    this.contactForm = this.fb.group({
      contactName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
      email: ['', [Validators.required, Validators.email, Validators.maxLength(255)]],
      organizationName: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(200)]],
      message: ['', [Validators.required, Validators.minLength(10), Validators.maxLength(2000)]]
    });
  }

  get f() {
    return this.contactForm.controls;
  }

  onSubmit(): void {
    if (this.contactForm.invalid) {
      this.contactForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    this.submitSuccess = false;
    this.submitError = null;

    this.publicApi.submitContactInquiry(this.contactForm.value).subscribe({
      next: (response) => {
        this.isSubmitting = false;
        this.submitSuccess = true;
        this.contactForm.reset();
        
        // Hide success message after 5 seconds
        setTimeout(() => {
          this.submitSuccess = false;
        }, 5000);
      },
      error: (error: HttpErrorResponse) => {
        this.isSubmitting = false;
        
        if (error.status === 429) {
          this.submitError = 'You have submitted too many inquiries. Please try again later.';
        } else if (error.status === 400) {
          this.submitError = 'Please check your form and try again.';
        } else if (error.status === 0) {
          this.submitError = 'Unable to connect to the server. Please check your internet connection.';
        } else {
          this.submitError = 'An error occurred while submitting your inquiry. Please try again later.';
        }
      }
    });
  }
}
