import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';
import { TenantService } from './tenant.service';
import { TenantContext, TenantTheme } from '../models/tenant.model';
import { vi } from 'vitest';

describe('ThemeService', () => {
  let service: ThemeService;
  let tenantService: TenantService;

  const mockTenantTheme: TenantTheme = {
    primaryColor: '#1E40AF',
    secondaryColor: '#64748B',
    logoUrl: 'https://example.com/logo.png'
  };

  const mockTenant: TenantContext = {
    id: 'tenant-123',
    subdomain: 'academy',
    name: 'Al-Azhar Academy',
    theme: mockTenantTheme,
    isActive: true
  };

  beforeEach(() => {
    const mockTenantService = {
      get currentTenant() { return mockTenant; }
    };

    TestBed.configureTestingModule({
      providers: [
        ThemeService,
        { provide: TenantService, useValue: mockTenantService }
      ]
    });

    service = TestBed.inject(ThemeService);
    tenantService = TestBed.inject(TenantService);
  });

  afterEach(() => {
    // Clean up CSS variables
    document.documentElement.style.removeProperty('--color-primary');
    document.documentElement.style.removeProperty('--color-secondary');
    document.documentElement.style.removeProperty('--logo-url');
  });

  it('should be created', () => {
    expect(service).toBeTruthy();
  });

  describe('Theme Loading', () => {
    it('should load theme from tenant context', async () => {
      await service.loadTheme();

      expect(service.currentTheme).toEqual(mockTenantTheme);
    });

    it('should apply CSS variables to document root', async () => {
      await service.loadTheme();

      const root = document.documentElement;
      expect(root.style.getPropertyValue('--color-primary')).toBe('#1E40AF');
      expect(root.style.getPropertyValue('--color-secondary')).toBe('#64748B');
      expect(root.style.getPropertyValue('--logo-url')).toBe('url(https://example.com/logo.png)');
    });

    it('should generate color variants (light and dark)', async () => {
      await service.loadTheme();

      const root = document.documentElement;
      expect(root.style.getPropertyValue('--color-primary-light')).toBeTruthy();
      expect(root.style.getPropertyValue('--color-primary-dark')).toBeTruthy();
      expect(root.style.getPropertyValue('--color-secondary-light')).toBeTruthy();
      expect(root.style.getPropertyValue('--color-secondary-dark')).toBeTruthy();
    });

    it('should emit theme through Observable', async () => {
      let emittedTheme: TenantTheme | null = null;
      
      const subscription = service.theme$.subscribe(theme => {
        emittedTheme = theme;
      });

      await service.loadTheme();

      expect(emittedTheme).toEqual(mockTenantTheme);
      
      subscription.unsubscribe();
    });
  });

  describe('Default Theme Fallback', () => {
    it('should apply default theme when tenant has no theme', async () => {
      vi.spyOn(tenantService, 'currentTenant', 'get').mockReturnValue({
        ...mockTenant,
        theme: {} as TenantTheme
      });

      await service.loadTheme();

      expect(service.currentTheme).toBeTruthy();
      expect(service.currentTheme?.primaryColor).toBe('#1E40AF'); // Default blue
    });

    it('should apply default theme when tenant is null', async () => {
      vi.spyOn(tenantService, 'currentTenant', 'get').mockReturnValue(null);

      await service.loadTheme();

      expect(service.currentTheme).toBeTruthy();
      expect(service.currentTheme?.primaryColor).toBe('#1E40AF');
    });
  });

  describe('No Static Tenant Rules', () => {
    it('should not contain hardcoded tenant-specific styles', async () => {
      // Constitution Rule: FE-029 (no hardcoded tenant-specific styles)
      
      await service.loadTheme();

      // Verify service doesn't have tenant-specific logic
      const serviceCode = service.constructor.toString();
      
      // Should not contain tenant-specific identifiers
      expect(serviceCode).not.toContain('academy');
      expect(serviceCode).not.toContain('university');
      expect(serviceCode).not.toContain('school');
    });

    it('should load theme dynamically from API data', async () => {
      // Theme should come from tenant context, not hardcoded
      const customTheme: TenantTheme = {
        primaryColor: '#FF0000',
        secondaryColor: '#00FF00',
        logoUrl: 'https://custom.com/logo.png'
      };

      vi.spyOn(tenantService, 'currentTenant', 'get').mockReturnValue({
        ...mockTenant,
        theme: customTheme
      });

      await service.loadTheme();

      expect(service.currentTheme).toEqual(customTheme);
      
      const root = document.documentElement;
      expect(root.style.getPropertyValue('--color-primary')).toBe('#FF0000');
      expect(root.style.getPropertyValue('--color-secondary')).toBe('#00FF00');
    });
  });

  describe('Theme Reset', () => {
    it('should reset theme to default', async () => {
      await service.loadTheme();
      
      expect(service.currentTheme).toEqual(mockTenantTheme);

      service.resetTheme();

      expect(service.currentTheme?.primaryColor).toBe('#1E40AF'); // Default
    });
  });

  describe('Color Manipulation', () => {
    it('should lighten colors correctly', async () => {
      await service.loadTheme();

      const root = document.documentElement;
      const lightColor = root.style.getPropertyValue('--color-primary-light');
      
      // Light color should be different from original
      expect(lightColor).not.toBe('#1E40AF');
      expect(lightColor).toBeTruthy();
    });

    it('should darken colors correctly', async () => {
      await service.loadTheme();

      const root = document.documentElement;
      const darkColor = root.style.getPropertyValue('--color-primary-dark');
      
      // Dark color should be different from original
      expect(darkColor).not.toBe('#1E40AF');
      expect(darkColor).toBeTruthy();
    });
  });

  describe('Partial Theme Support', () => {
    it('should handle theme with only primary color', async () => {
      vi.spyOn(tenantService, 'currentTenant', 'get').mockReturnValue({
        ...mockTenant,
        theme: { primaryColor: '#FF0000' }
      });

      await service.loadTheme();

      const root = document.documentElement;
      expect(root.style.getPropertyValue('--color-primary')).toBe('#FF0000');
    });

    it('should handle theme with no logo', async () => {
      vi.spyOn(tenantService, 'currentTenant', 'get').mockReturnValue({
        ...mockTenant,
        theme: {
          primaryColor: '#1E40AF',
          secondaryColor: '#64748B'
        }
      });

      await service.loadTheme();

      expect(service.currentTheme?.logoUrl).toBeUndefined();
    });
  });
});
