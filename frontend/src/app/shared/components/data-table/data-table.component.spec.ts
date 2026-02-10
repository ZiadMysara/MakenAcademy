import { ComponentFixture, TestBed } from '@angular/core/testing';
import { DataTableComponent, DataTableColumn } from './data-table.component';
import { DebugElement } from '@angular/core';
import { By } from '@angular/platform-browser';
import { vi } from 'vitest';

describe('DataTableComponent', () => {
  let component: DataTableComponent;
  let fixture: ComponentFixture<DataTableComponent>;
  let compiled: DebugElement;

  const mockColumns: DataTableColumn[] = [
    { label: 'Name', key: 'name' },
    { label: 'Status', key: 'status', align: 'center' },
    { label: 'Count', key: 'count', align: 'right' }
  ];

  const mockData = [
    { id: 1, name: 'Course 1', status: 'Active', count: 10 },
    { id: 2, name: 'Course 2', status: 'Inactive', count: 5 }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [DataTableComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(DataTableComponent);
    component = fixture.componentInstance;
    compiled = fixture.debugElement;
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });

  describe('Empty State', () => {
    it('should show empty state when data is empty array', () => {
      component.columns = mockColumns;
      component.data = [];
      fixture.detectChanges();

      expect(component.isEmpty).toBe(true);
      const emptyElement = compiled.query(By.css('.table-empty'));
      expect(emptyElement).toBeTruthy();
      expect(emptyElement.nativeElement.textContent).toContain('No data to display');
    });

    it('should show empty state when data is null', () => {
      component.columns = mockColumns;
      component.data = null as any;
      fixture.detectChanges();

      expect(component.isEmpty).toBe(true);
      const emptyElement = compiled.query(By.css('.table-empty'));
      expect(emptyElement).toBeTruthy();
    });

    it('should not show table when empty', () => {
      component.columns = mockColumns;
      component.data = [];
      fixture.detectChanges();

      const tableElement = compiled.query(By.css('.data-table'));
      expect(tableElement).toBeFalsy();
    });
  });

  describe('Table Rendering', () => {
    beforeEach(() => {
      component.columns = mockColumns;
      component.data = mockData;
      fixture.detectChanges();
    });

    it('should render table when data is provided', () => {
      const tableElement = compiled.query(By.css('.data-table'));
      expect(tableElement).toBeTruthy();
    });

    it('should render correct number of column headers', () => {
      const headers = compiled.queryAll(By.css('thead th'));
      expect(headers.length).toBe(mockColumns.length);
    });

    it('should render column headers with correct labels', () => {
      const headers = compiled.queryAll(By.css('thead th'));
      expect(headers[0].nativeElement.textContent.trim()).toBe('Name');
      expect(headers[1].nativeElement.textContent.trim()).toBe('Status');
      expect(headers[2].nativeElement.textContent.trim()).toBe('Count');
    });

    it('should render correct number of rows', () => {
      const rows = compiled.queryAll(By.css('tbody tr'));
      expect(rows.length).toBe(mockData.length);
    });

    it('should render cell values correctly', () => {
      const firstRowCells = compiled.queryAll(By.css('tbody tr:first-child td'));
      expect(firstRowCells[0].nativeElement.textContent.trim()).toBe('Course 1');
      expect(firstRowCells[1].nativeElement.textContent.trim()).toBe('Active');
      expect(firstRowCells[2].nativeElement.textContent.trim()).toBe('10');
    });

    it('should apply text alignment from column definition', () => {
      const headers = compiled.queryAll(By.css('thead th'));
      expect(headers[1].nativeElement.style.textAlign).toBe('center');
      expect(headers[2].nativeElement.style.textAlign).toBe('right');
    });
  });

  describe('Cell Value Extraction', () => {
    it('should get simple property value', () => {
      const row = { name: 'Test' };
      const value = component.getCellValue(row, 'name');
      expect(value).toBe('Test');
    });

    it('should get nested property value', () => {
      const row = { user: { name: 'John' } };
      const value = component.getCellValue(row, 'user.name');
      expect(value).toBe('John');
    });

    it('should return empty string for missing property', () => {
      const row = { name: 'Test' };
      const value = component.getCellValue(row, 'missing');
      expect(value).toBe('');
    });

    it('should return empty string for null object', () => {
      const value = component.getCellValue(null, 'name');
      expect(value).toBe('');
    });
  });

  describe('Actions', () => {
    beforeEach(() => {
      component.columns = mockColumns;
      component.data = mockData;
      component.actions = ['edit', 'delete'];
      fixture.detectChanges();
    });

    it('should render actions column header when actions are provided', () => {
      const headers = compiled.queryAll(By.css('thead th'));
      const actionsHeader = headers[headers.length - 1];
      expect(actionsHeader.nativeElement.textContent.trim()).toBe('Actions');
    });

    it('should render action buttons for each row', () => {
      const firstRowActions = compiled.queryAll(By.css('tbody tr:first-child .action-button'));
      expect(firstRowActions.length).toBe(2);
    });

    it('should emit actionClick event when action button is clicked', () => {
      vi.spyOn(component.actionClick, 'emit');
      
      const editButton = compiled.query(By.css('.action-edit'));
      editButton.nativeElement.click();

      expect(component.actionClick.emit).toHaveBeenCalledWith({
        action: 'edit',
        row: mockData[0]
      });
    });

    it('should get correct action labels', () => {
      expect(component.getActionLabel('edit')).toBe('Edit');
      expect(component.getActionLabel('delete')).toBe('Delete');
      expect(component.getActionLabel('view')).toBe('View');
      expect(component.getActionLabel('custom')).toBe('custom');
    });

    it('should get correct action icons', () => {
      expect(component.getActionIcon('edit')).toBe('✏️');
      expect(component.getActionIcon('delete')).toBe('🗑️');
      expect(component.getActionIcon('view')).toBe('👁️');
      expect(component.getActionIcon('custom')).toBe('•');
    });
  });

  describe('Responsive Behavior', () => {
    it('should add data-label attribute for mobile view', () => {
      component.columns = mockColumns;
      component.data = mockData;
      fixture.detectChanges();

      const firstCell = compiled.query(By.css('tbody tr:first-child td:first-child'));
      expect(firstCell.nativeElement.getAttribute('data-label')).toBe('Name');
    });
  });
});
