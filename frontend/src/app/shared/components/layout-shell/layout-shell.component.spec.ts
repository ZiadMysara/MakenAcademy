import { ComponentFixture, TestBed } from '@angular/core/testing';
import { LayoutShellComponent } from './layout-shell.component';
import { provideRouter } from '@angular/router';

describe('LayoutShellComponent', () => {
  let component: LayoutShellComponent;
  let fixture: ComponentFixture<LayoutShellComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [LayoutShellComponent],
      providers: [provideRouter([])]
    }).compileComponents();

    fixture = TestBed.createComponent(LayoutShellComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  it('should toggle sidebar', () => {
    expect(component.isSidebarOpen).toBe(false);
    
    component.toggleSidebar();
    expect(component.isSidebarOpen).toBe(true);
    
    component.toggleSidebar();
    expect(component.isSidebarOpen).toBe(false);
  });

  it('should render header', () => {
    const compiled = fixture.nativeElement;
    const header = compiled.querySelector('.header');
    expect(header).toBeTruthy();
  });

  it('should render sidebar', () => {
    const compiled = fixture.nativeElement;
    const sidebar = compiled.querySelector('.sidebar');
    expect(sidebar).toBeTruthy();
  });

  it('should render main content area', () => {
    const compiled = fixture.nativeElement;
    const main = compiled.querySelector('.main-content');
    expect(main).toBeTruthy();
  });

  it('should have router outlet', () => {
    const compiled = fixture.nativeElement;
    const outlet = compiled.querySelector('router-outlet');
    expect(outlet).toBeTruthy();
  });
});
