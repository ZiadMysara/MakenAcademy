import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';

/**
 * Layout Shell Component
 * Provides the main application layout structure
 * Constitution Rules: FE-030, FE-031, FE-033
 * 
 * Structure:
 * - Header (top navigation)
 * - Sidebar (collapsible navigation)
 * - Content area (router outlet)
 * 
 * Mobile-first responsive design (Constitution §12.I)
 */
@Component({
  selector: 'app-layout-shell',
  standalone: true,
  imports: [CommonModule, RouterModule],
  templateUrl: './layout-shell.component.html',
  styleUrl: './layout-shell.component.css'
})
export class LayoutShellComponent {
  isSidebarOpen = false;

  toggleSidebar(): void {
    this.isSidebarOpen = !this.isSidebarOpen;
  }
}
