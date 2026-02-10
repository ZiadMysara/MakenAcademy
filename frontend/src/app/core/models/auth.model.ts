/**
 * Authentication State Model
 * Represents the current authentication status and user information
 * Constitution Rule: FE-020, FE-021, FE-024
 */
export interface AuthState {
  /** Whether the user is authenticated */
  isAuthenticated: boolean;

  /** Current user information (null if not authenticated) */
  user: User | null;

  /** JWT access token (null if not authenticated) */
  token: string | null;

  /** User role for UI display (no authorization decisions in frontend) */
  role: UserRole | null;
}

/**
 * User Information
 * Basic user data exposed to the frontend
 */
export interface User {
  /** User unique identifier */
  id: string;

  /** User email address */
  email: string;

  /** User display name */
  name: string;

  /** Tenant ID the user belongs to */
  tenantId: string;
}

/**
 * User Roles
 * Constitution Rule: FR-017 (from spec 001-global-rules)
 */
export enum UserRole {
  PlatformAdmin = 'PlatformAdmin',
  CompanyAdmin = 'CompanyAdmin',
  Instructor = 'Instructor',
  Student = 'Student'
}

/**
 * Login Credentials
 * Email/password only (No OAuth per Constitution Patch I)
 */
export interface LoginCredentials {
  email: string;
  password: string;
}

/**
 * Login Response from Backend API
 */
export interface LoginResponse {
  user: User;
  token: string;
  refreshToken: string;
  expiresIn: number; // Token expiry in seconds
}
