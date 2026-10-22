import { Component, output } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl, FormGroup, ReactiveFormsModule } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { debounceTime, distinctUntilChanged, map } from 'rxjs';
import {
  REQUEST_PRIORITIES,
  REQUEST_STATUSES,
  RequestFilters,
  RequestPriority,
  RequestStatus
} from '../../../core/models/service-request.models';
import { PriorityLabelPipe, StatusLabelPipe } from '../../../shared/request-labels';

const FILTER_DEBOUNCE_MS = 300;

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
export class RequestFiltersComponent {
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
    this.form.valueChanges
      .pipe(
        // Wait until the user stops typing for 300ms instead of sending a request per keystroke.
        debounceTime(FILTER_DEBOUNCE_MS),
        map(() => this.currentFilters()),
        // Skip values that end up identical to the previous ones (e.g. "ab" → "abc" → "ab", or trailing spaces).
        distinctUntilChanged((previous, current) => JSON.stringify(previous) === JSON.stringify(current)),
        takeUntilDestroyed()
      )
      .subscribe(filters => this.filtersChange.emit(filters));
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
