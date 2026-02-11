import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { ContactInquiryService, ContactInquiry } from '../services/contact-inquiry.service';

@Component({
  selector: 'app-contact-inquiry-detail',
  imports: [CommonModule, FormsModule],
  templateUrl: './contact-inquiry-detail.html',
  styleUrl: './contact-inquiry-detail.css',
})
export class ContactInquiryDetail implements OnInit {
  private readonly contactInquiryService = inject(ContactInquiryService);
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);

  inquiry: ContactInquiry | null = null;
  isLoading = true;
  error: string | null = null;
  
  selectedStatus: string = '';
  notes: string = '';
  isUpdating = false;
  updateSuccess = false;
  updateError: string | null = null;

  ngOnInit(): void {
    const id = this.route.snapshot.paramMap.get('id');
    if (id) {
      this.loadInquiry(id);
    }
  }

  loadInquiry(id: string): void {
    // For now, we'll navigate back since we don't have a GET single inquiry endpoint
    // In a real implementation, you'd add a getInquiryById method to the service
    this.router.navigate(['/admin/contact-inquiries']);
  }

  updateStatus(): void {
    if (!this.inquiry) return;

    this.isUpdating = true;
    this.updateSuccess = false;
    this.updateError = null;

    this.contactInquiryService
      .updateInquiryStatus(this.inquiry.id, {
        status: this.selectedStatus,
        notes: this.notes || undefined
      })
      .subscribe({
        next: () => {
          this.isUpdating = false;
          this.updateSuccess = true;
          setTimeout(() => {
            this.router.navigate(['/admin/contact-inquiries']);
          }, 1500);
        },
        error: (err) => {
          this.isUpdating = false;
          this.updateError = 'Failed to update inquiry status';
        }
      });
  }

  goBack(): void {
    this.router.navigate(['/admin/contact-inquiries']);
  }
}
