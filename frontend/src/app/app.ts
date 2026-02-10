import { Component, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';

/**
 * Root Application Component
 * Constitution Rule: FE-034, FE-035 (surface separation)
 * 
 * Uses router outlet for surface isolation
 * Layout shell is applied within each surface route
 */
@Component({
  selector: 'app-root',
  imports: [RouterOutlet],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('maken-frontend');
}
