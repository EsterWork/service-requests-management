import { Component, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { DatePipe, DecimalPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { catchError, map, of, scan, startWith, switchMap } from 'rxjs';
import {
  DEFAULT_QUERY,
  PagedResult,
  RequestFilters,
  RequestQuery,
  ServiceRequest,
  SortField
} from '../../../core/models/service-request.models';
import { RequestsApiService } from '../../../core/services/requests-api.service';
import { PriorityLabelPipe, StatusLabelPipe } from '../../../shared/request-labels';
import { RequestFiltersComponent } from '../request-filters/request-filters.component';

interface ListState {
  loading: boolean;
  error: boolean;
  result: PagedResult<ServiceRequest> | null;
}

const INITIAL_STATE: ListState = { loading: true, error: false, result: null };

@Component({
  selector: 'app-request-list',
  standalone: true,
  imports: [
    DatePipe,
    DecimalPipe,
    MatButtonModule,
    MatIconModule,
    MatPaginatorModule,
    MatProgressBarModule,
    MatProgressSpinnerModule,
    MatSortModule,
    MatTableModule,
    PriorityLabelPipe,
    StatusLabelPipe,
    RequestFiltersComponent
  ],
  templateUrl: './request-list.component.html',
  styleUrl: './request-list.component.scss'
})
export class RequestListComponent {
  private readonly api = inject(RequestsApiService);

  readonly columns = ['title', 'organizationName', 'status', 'priority', 'assignedTo', 'createdAt', 'updatedAt'];
  readonly pageSizeOptions = [10, 20, 50, 100];

  /** Single source of truth for what is shown: filters, sort and page. Every change triggers a server request. */
  readonly query = signal<RequestQuery>(DEFAULT_QUERY);

  readonly state = toSignal(
    toObservable(this.query).pipe(
      // switchMap cancels the in-flight HTTP request when a newer query arrives,
      // so a slow, outdated response can never overwrite the latest results.
      switchMap(query =>
        this.api.getRequests(query).pipe(
          map(result => ({ loading: false, error: false, result })),
          catchError(() => of({ loading: false, error: true })),
          startWith({ loading: true, error: false })
        )
      ),
      // Keep the previous page visible while the next one loads.
      scan((state: ListState, change: Partial<ListState>) => ({ ...state, ...change }), INITIAL_STATE)
    ),
    { initialValue: INITIAL_STATE }
  );

  onFiltersChange(filters: RequestFilters): void {
    this.query.update(query => ({ ...query, ...filters, page: 1 }));
  }

  onSortChange(sort: Sort): void {
    this.query.update(query => ({
      ...query,
      sortBy: sort.active as SortField,
      sortDirection: sort.direction || 'desc',
      page: 1
    }));
  }

  onPageChange(event: PageEvent): void {
    this.query.update(query => ({ ...query, page: event.pageIndex + 1, pageSize: event.pageSize }));
  }

  retry(): void {
    this.query.update(query => ({ ...query }));
  }
}
