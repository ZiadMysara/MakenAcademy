import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../../environments/environment';

export interface ContactInquiry {
  id: string;
  contactName: string;
  email: string;
  organizationName: string;
  message: string;
  status: string;
  submittedAt: string;
  reviewedAt: string | null;
  reviewedBy: string | null;
  notes: string | null;
}

export interface ContactInquiryListResponse {
  items: ContactInquiry[];
  totalCount: number;
  pageNumber: number;
  pageSize: number;
  totalPages: number;
}

export interface UpdateStatusRequest {
  status: string;
  notes?: string;
}

@Injectable({
  providedIn: 'root',
})
export class ContactInquiryService {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = `${environment.apiBaseUrl}/contact-inquiries`;

  getContactInquiries(
    pageNumber: number = 1,
    pageSize: number = 20,
    status?: string
  ): Observable<ContactInquiryListResponse> {
    let params = new HttpParams()
      .set('pageNumber', pageNumber.toString())
      .set('pageSize', pageSize.toString());

    if (status) {
      params = params.set('status', status);
    }

    return this.http.get<ContactInquiryListResponse>(this.apiUrl, { params });
  }

  updateInquiryStatus(id: string, request: UpdateStatusRequest): Observable<void> {
    return this.http.patch<void>(`${this.apiUrl}/${id}/status`, request);
  }
}
