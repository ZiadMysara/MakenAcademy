import { Component, inject, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { PublicApi, PublicOrganization } from '../../services/public-api';

@Component({
  selector: 'app-organization-showcase',
  imports: [CommonModule],
  templateUrl: './organization-showcase.html',
  styleUrl: './organization-showcase.css',
})
export class OrganizationShowcase implements OnInit {
  private readonly publicApi = inject(PublicApi);
  
  organizations: PublicOrganization[] = [];
  loading = true;
  error: string | null = null;

  ngOnInit(): void {
    this.loadOrganizations();
  }

  private loadOrganizations(): void {
    this.loading = true;
    this.error = null;

    this.publicApi.getOrganizations().subscribe({
      next: (orgs) => {
        this.organizations = orgs;
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to load organizations:', err);
        this.error = 'Failed to load organizations. Please try again later.';
        this.loading = false;
      }
    });
  }

  onOrganizationClick(organization: PublicOrganization): void {
    // Navigate to tenant subdomain
    const protocol = window.location.protocol;
    const hostname = window.location.hostname;
    const port = window.location.port;
    
    // For local development, use subdomain.localhost:port
    // For production, use subdomain.maken.app
    let targetUrl: string;
    
    if (hostname === 'localhost' || hostname === '127.0.0.1') {
      // Local development: subdomain.localhost:port
      targetUrl = `${protocol}//${organization.subdomain}.${hostname}${port ? ':' + port : ''}`;
    } else {
      // Production: subdomain.maken.app
      targetUrl = `${protocol}//${organization.subdomain}.maken.app`;
    }
    
    window.location.href = targetUrl;
  }
}
