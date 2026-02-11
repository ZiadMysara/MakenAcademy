import { Component, OnInit, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { ContactInquiryService, ContactInquiry } from '../services/contact-inquiry.service';

@Component({
  selector: 'app-contact-inquiry-list',
  imports: [CommonModule, FormsModule],
  templateUrl: './contact-inquiry-list.html',
  styleUrl: './contact-inquiry-list.css',
})
export class ContactInquiryList implements OnInit {
  private readonly contactInquiryService = inject(ContactInquiryService);
  private readonly router = inject(Router);

  inquiries: ContactInquiry[] = [];
  isLoading = true;
  error: string | null = null;

  // Pagination
  currentPage = 1;
  pageSize = 20;
  totalCount = 0;
  totalPages = 0;

  // Filter
  selectedStatus: string = '';

  ngOnInit(): void {
    this.loadInquiries();
  }

  loadInquiries(): void {
    this.isLoading = true;
    this.error = null;

    this.contactInquiryService
      .getContactInquiries(this.currentPage, this.pageSize, this.selectedStatus || undefined)
      .subscribe({
        next: (response) => {
          this.inquiries = response.items;
          this.totalCount = response.totalCount;
          this.totalPages = response.totalPages;
          this.currentPage = response.pageNumber;
          this.isLoading = false;
        },
        error: (err) => {
          this.error = 'Failed to load contact inquiries';
          this.isLoading = false;
        }
      });
  }

  onStatusFilterChange(status: string): void {
    this.selectedStatus = status;
    this.currentPage = 1;
    this.loadInquiries();
  }

  onPageChange(page: number): void {
    this.currentPage = page;
    this.loadInquiries();
  }

  viewInquiry(id: string): void {
    this.router.navigate(['/admin/contact-inquiries', id]);
  }

  getStatusClass(status: string): string {
    switch (status) {
      case 'New':
        return 'status-new';
      case 'Reviewed':
        return 'status-reviewed';
      case 'Contacted':
        return 'status-contacted';
      default:
        return '';
    }
  }
}
