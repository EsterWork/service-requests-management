import { Component, inject, signal, viewChild } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { DatePipe, DecimalPipe, Location } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatPaginatorModule, PageEvent } from '@angular/material/paginator';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { MatSnackBar } from '@angular/material/snack-bar';
import { MatSortModule, Sort } from '@angular/material/sort';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { catchError, map, of, scan, startWith, switchMap, tap } from 'rxjs';
import {
  ALLOWED_TRANSITIONS,
  PagedResult,
  RequestFilters,
  RequestQuery,
  ServiceRequest,
  SortField
} from '../../../core/models/service-request.models';
import { RequestsApiService } from '../../../core/services/requests-api.service';
import { PriorityLabelPipe, StatusLabelPipe } from '../../../shared/request-labels';
import { RequestFiltersComponent } from '../request-filters/request-filters.component';
import { PAGE_SIZE_OPTIONS, readQueryFromUrl, toUrlQueryString } from '../request-query-url';
import {
  RequestHistoryDialogComponent,
  RequestHistoryDialogData
} from '../request-history-dialog/request-history-dialog.component';
import { RequestSummaryComponent } from '../request-summary/request-summary.component';
import {
  StatusUpdateComponent,
  StatusUpdateDialogData,
  StatusUpdateOutcome
} from '../status-update/status-update.component';

interface ListState {
  loading: boolean;
  error: boolean;
  result: PagedResult<ServiceRequest> | null;
}

const INITIAL_STATE: ListState = { loading: true, error: false, result: null };

const STATUS_UPDATE_MESSAGES: Record<StatusUpdateOutcome, string> = {
  updated: 'הסטטוס עודכן בהצלחה.',
  conflict: 'הפנייה עודכנה על ידי משתמש אחר. הנתונים רועננו — בדוק את המצב העדכני ונסה שוב.',
  notFound: 'הפנייה לא נמצאה. הנתונים רועננו.'
};

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
    MatTooltipModule,
    PriorityLabelPipe,
    StatusLabelPipe,
    RequestFiltersComponent,
    RequestSummaryComponent
  ],
  templateUrl: './request-list.component.html',
  styleUrl: './request-list.component.scss'
})
export class RequestListComponent {
  private readonly api = inject(RequestsApiService);
  private readonly dialog = inject(MatDialog);
  private readonly snackBar = inject(MatSnackBar);
  private readonly summary = viewChild.required(RequestSummaryComponent);

  private readonly location = inject(Location);

  readonly columns = ['title', 'organizationName', 'status', 'priority', 'assignedTo', 'createdAt', 'updatedAt', 'actions'];
  readonly pageSizeOptions = PAGE_SIZE_OPTIONS;

  /**
   * Single source of truth for what is shown: filters, sort and page. Every change triggers a server request.
   * It starts from the URL and is written back to it, so a refresh or a shared link keeps the same view.
   */
  readonly query = signal<RequestQuery>(readQueryFromUrl(this.location.path().split('?')[1] ?? ''));
  readonly initialFilters: RequestFilters = this.query();

  readonly state = toSignal(
    toObservable(this.query).pipe(
      tap(query => this.location.replaceState(this.location.path().split('?')[0], toUrlQueryString(query))),
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

  /** Re-runs the current query (same filters, sort and page). */
  reload(): void {
    this.query.update(query => ({ ...query }));
  }

  canUpdateStatus(request: ServiceRequest): boolean {
    return ALLOWED_TRANSITIONS[request.status].length > 0;
  }

  openStatusUpdate(request: ServiceRequest): void {
    this.dialog
      .open<StatusUpdateComponent, StatusUpdateDialogData, StatusUpdateOutcome>(StatusUpdateComponent, { data: { request } })
      .afterClosed()
      .subscribe(outcome => {
        if (!outcome) {
          return;
        }
        this.snackBar.open(STATUS_UPDATE_MESSAGES[outcome], 'סגירה', { duration: outcome === 'updated' ? 3000 : 8000 });
        // After success the row and the summary changed; after a conflict the shown data is outdated.
        this.reload();
        this.summary().refresh();
      });
  }

  openHistory(request: ServiceRequest): void {
    this.dialog.open<RequestHistoryDialogComponent, RequestHistoryDialogData>(RequestHistoryDialogComponent, {
      data: { request },
      width: '560px'
    });
  }
}
