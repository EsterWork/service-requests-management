import { Component, OnInit, input, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { debounce, distinctUntilChanged, map, merge, timer } from 'rxjs';
import {
  EMPTY_FILTERS,
  REQUEST_PRIORITIES,
  REQUEST_STATUSES,
  RequestFilters,
  RequestPriority,
  RequestStatus
} from '../../../core/models/service-request.models';
import { PriorityLabelPipe, StatusLabelPipe } from '../../../shared/request-labels';

export const TEXT_DEBOUNCE_MS = 300;

@Component({
  selector: 'app-request-filters',
  standalone: true,
  imports: [
    ReactiveFormsModule,
    MatButtonModule,
    MatFormFieldModule,
    MatIconModule,
    MatInputModule,
    MatSelectModule,
    StatusLabelPipe,
    PriorityLabelPipe
  ],
  templateUrl: './request-filters.component.html',
  styleUrl: './request-filters.component.scss'
})
export class RequestFiltersComponent implements OnInit {
  /** Filters to show when the page opens (e.g. restored from the URL). */
  readonly initialFilters = input<RequestFilters>(EMPTY_FILTERS);
  readonly filtersChange = output<RequestFilters>();

  readonly statuses = REQUEST_STATUSES;
  readonly priorities = REQUEST_PRIORITIES;

  readonly form = new FormGroup({
    search: new FormControl('', { nonNullable: true }),
    status: new FormControl<RequestStatus | null>(null),
    priority: new FormControl<RequestPriority | null>(null),
    organizationName: new FormControl('', { nonNullable: true }),
    assignedTo: new FormControl('', { nonNullable: true }),
    createdFrom: new FormControl('', { nonNullable: true }),
    createdTo: new FormControl('', { nonNullable: true })
  });

  constructor() {
    const { search, organizationName, assignedTo, status, priority, createdFrom, createdTo } = this.form.controls;
    const textControls: AbstractControl[] = [search, organizationName, assignedTo];
    const choiceControls: AbstractControl[] = [status, priority, createdFrom, createdTo];

    merge(
      // Typing waits until the user pauses for 300ms instead of sending a request per keystroke;
      // selects and dates are a single deliberate choice, so they apply immediately.
      ...textControls.map(control => control.valueChanges.pipe(map(() => TEXT_DEBOUNCE_MS))),
      ...choiceControls.map(control => control.valueChanges.pipe(map(() => 0)))
    )
      .pipe(
        debounce(delay => timer(delay)),
        map(() => this.currentFilters()),
        // Skip values that end up identical to the previous ones (e.g. "ab" → "abc" → "ab", or trailing spaces).
        distinctUntilChanged((previous, current) => JSON.stringify(previous) === JSON.stringify(current)),
        takeUntilDestroyed()
      )
      .subscribe(filters => this.filtersChange.emit(filters));
  }

  ngOnInit(): void {
    const filters = this.initialFilters();
    this.form.setValue(
      {
        search: filters.search,
        status: filters.status,
        priority: filters.priority,
        organizationName: filters.organizationName,
        assignedTo: filters.assignedTo,
        createdFrom: toDateInputValue(filters.createdFrom),
        createdTo: toDateInputValue(filters.createdTo)
      },
      { emitEvent: false }
    );
  }

  clear(): void {
    this.form.reset();
  }

  private currentFilters(): RequestFilters {
    const value = this.form.getRawValue();
    return {
      search: value.search.trim(),
      status: value.status,
      priority: value.priority,
      organizationName: value.organizationName.trim(),
      assignedTo: value.assignedTo.trim(),
      // The date inputs are local days; the API receives the exact UTC range of that local day.
      createdFrom: value.createdFrom ? new Date(`${value.createdFrom}T00:00:00`).toISOString() : null,
      createdTo: value.createdTo ? new Date(`${value.createdTo}T23:59:59.999`).toISOString() : null
    };
  }
}

/** ISO timestamp → "yyyy-MM-dd" of the local day, the format of <input type="date">. */
function toDateInputValue(iso: string | null): string {
  if (!iso) {
    return '';
  }
  const date = new Date(iso);
  const pad = (value: number) => String(value).padStart(2, '0');
  return `${date.getFullYear()}-${pad(date.getMonth() + 1)}-${pad(date.getDate())}`;
}
