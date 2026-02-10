/**
 * Tenant Context Model
 * Represents the resolved tenant information from subdomain
 * Constitution Rule: FE-014, FE-015, FE-016
 */
export interface TenantContext {
  /** Unique identifier for the tenant */
  id: string;

  /** Subdomain used to access the tenant (e.g., "academy" from academy.maken.app) */
  subdomain: string;

  /** Display name of the tenant */
  name: string;

  /** Theme configuration for the tenant */
  theme: TenantTheme;

  /** Whether the tenant is active */
  isActive: boolean;
}

/**
 * Tenant Theme Configuration
 * Loaded at runtime from API and applied via CSS variables
 * Constitution Rule: FE-028, FE-029
 */
export interface TenantTheme {
  /** Primary color in hex format (e.g., "#1E40AF") */
  primaryColor?: string;

  /** Secondary color in hex format (e.g., "#64748B") */
  secondaryColor?: string;

  /** URL to tenant logo */
  logoUrl?: string;
}
