import { Injectable, inject } from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';

export interface PublicOrganization {
  id: string;
  name: string;
  subdomain: string;
  logoUrl: string | null;
  primaryColor: string | null;
  secondaryColor: string | null;
}

@Injectable({
  providedIn: 'root',
})
export class PublicApi {
  private readonly http = inject(HttpClient);
  private readonly apiUrl = environment.apiBaseUrl;

  getOrganizations(): Observable<PublicOrganization[]> {
    return this.http.get<PublicOrganization[]>(`${this.apiUrl}/public/organizations`);
  }
}
