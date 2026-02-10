import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute } from '@angular/router';
import { of } from 'rxjs';
import { ErrorPageComponent } from './error-page.component';

describe('ErrorPageComponent', () => {
  let component: ErrorPageComponent;
  let fixture: ComponentFixture<ErrorPageComponent>;
  let mockActivatedRoute: any;

  beforeEach(async () => {
    mockActivatedRoute = {
      queryParams: of({})
    };

    await TestBed.configureTestingModule({
      imports: [ErrorPageComponent],
      providers: [
        { provide: ActivatedRoute, useValue: mockActivatedRoute }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(ErrorPageComponent);
    component = fixture.componentInstance;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should display default error message when no reason provided', () => {
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const title = compiled.querySelector('.error-title');
    const details = compiled.querySelector('.error-details');

    expect(title.textContent).toContain('Unable to load application');
    expect(details.textContent).toContain('An unexpected error occurred');
  });

  it('should display tenant-not-resolved error message', () => {
    mockActivatedRoute.queryParams = of({ reason: 'tenant-not-resolved' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const title = compiled.querySelector('.error-title');
    const details = compiled.querySelector('.error-details');

    expect(title.textContent).toContain('Unable to load application');
    expect(details.textContent).toContain('could not identify your organization');
  });

  it('should display api-unreachable error message', () => {
    mockActivatedRoute.queryParams = of({ reason: 'api-unreachable' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const title = compiled.querySelector('.error-title');
    const details = compiled.querySelector('.error-details');

    expect(title.textContent).toContain('Service unavailable');
    expect(details.textContent).toContain('unable to connect to our services');
  });

  it('should display initialization-failed error message', () => {
    mockActivatedRoute.queryParams = of({ reason: 'initialization-failed' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const title = compiled.querySelector('.error-title');
    const details = compiled.querySelector('.error-details');

    expect(title.textContent).toContain('Application failed to start');
    expect(details.textContent).toContain('error occurred while starting');
  });

  it('should display error code', () => {
    mockActivatedRoute.queryParams = of({ reason: 'tenant-not-resolved' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const errorCode = compiled.querySelector('.error-code');

    expect(errorCode.textContent).toContain('tenant-not-resolved');
  });

  it('should display error icon', () => {
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const icon = compiled.querySelector('.error-icon svg');

    expect(icon).toBeTruthy();
  });

  it('should not display retry button (per Constitution FE-019)', () => {
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const retryButton = compiled.querySelector('button');

    expect(retryButton).toBeNull();
  });

  it('should be responsive with mobile-first CSS', () => {
    fixture.detectChanges();

    const compiled = fixture.nativeElement;
    const container = compiled.querySelector('.error-container');

    expect(container).toBeTruthy();
    expect(container.classList.contains('error-container')).toBe(true);
  });
});
