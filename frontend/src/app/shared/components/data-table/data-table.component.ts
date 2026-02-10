import { Component, Input, Output, EventEmitter } from '@angular/core';
import { CommonModule } from '@angular/common';

/**
 * Column definition for DataTable
 */
export interface DataTableColumn {
  /** Column header label */
  label: string;
  /** Property key to access in row data */
  key: string;
  /** Optional: Custom width (e.g., '100px', '20%') */
  width?: string;
  /** Optional: Text alignment */
  align?: 'left' | 'center' | 'right';
}

/**
 * Row action definition
 */
export interface DataTableAction {
  /** Action identifier (e.g., 'edit', 'delete', 'view') */
  action: string;
  /** Row data */
  row: any;
}

/**
 * DataTableComponent
 * 
 * Generic table component for listing entities (courses, lessons, exams).
 * Accepts column definitions and row data via inputs, emits row actions via outputs.
 * 
 * Constitution Compliance:
 * - [FEAT-014] Reusable across both surfaces
 * - [FEAT-015] Composable component
 * - [FEAT-018] Data via inputs, events via outputs
 * - [FEAT-025] Handles empty state explicitly
 * - Mobile-first responsive (Constitution §12.I)
 * 
 * Usage:
 * <app-data-table 
 *   [columns]="columns" 
 *   [data]="rows"
 *   [actions]="['edit', 'delete']"
 *   (actionClick)="onAction($event)">
 * </app-data-table>
 */
@Component({
  selector: 'app-data-table',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './data-table.component.html',
  styleUrls: ['./data-table.component.css']
})
export class DataTableComponent {
  /**
   * Column definitions
   */
  @Input() columns: DataTableColumn[] = [];

  /**
   * Row data array
   */
  @Input() data: any[] = [];

  /**
   * Available actions for each row (e.g., ['edit', 'delete', 'view'])
   */
  @Input() actions: string[] = [];

  /**
   * Emitted when an action button is clicked
   */
  @Output() actionClick = new EventEmitter<DataTableAction>();

  /**
   * Handles action button click
   */
  onActionClick(action: string, row: any): void {
    this.actionClick.emit({ action, row });
  }

  /**
   * Gets the display value for a cell
   */
  getCellValue(row: any, key: string): any {
    // Support nested keys (e.g., 'user.name')
    return key.split('.').reduce((obj, k) => obj?.[k], row) ?? '';
  }

  /**
   * Checks if data is empty
   */
  get isEmpty(): boolean {
    return !this.data || this.data.length === 0;
  }

  /**
   * Gets action button label
   */
  getActionLabel(action: string): string {
    const labels: Record<string, string> = {
      'edit': 'Edit',
      'delete': 'Delete',
      'view': 'View',
      'reorder': 'Reorder'
    };
    return labels[action] || action;
  }

  /**
   * Gets action button icon
   */
  getActionIcon(action: string): string {
    const icons: Record<string, string> = {
      'edit': '✏️',
      'delete': '🗑️',
      'view': '👁️',
      'reorder': '↕️'
    };
    return icons[action] || '•';
  }
}
