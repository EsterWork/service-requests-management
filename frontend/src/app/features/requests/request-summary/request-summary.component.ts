import { Component, computed, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DecimalPipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { catchError, map, of, startWith, Subject, switchMap } from 'rxjs';
import { RequestSummary } from '../../../core/models/service-request.models';
import { RequestsApiService } from '../../../core/services/requests-api.service';
import { PriorityLabelPipe, StatusLabelPipe } from '../../../shared/request-labels';

interface SummaryState {
  summary: RequestSummary | null;
  error: boolean;
}

@Component({
  selector: 'app-request-summary',
  standalone: true,
  imports: [DecimalPipe, MatButtonModule, PriorityLabelPipe, StatusLabelPipe],
  templateUrl: './request-summary.component.html',
  styleUrl: './request-summary.component.scss'
})
export class RequestSummaryComponent {
  private readonly api = inject(RequestsApiService);
  private readonly refresh$ = new Subject<void>();

  // The previous numbers stay visible until the refreshed ones arrive.
  readonly state = toSignal(
    this.refresh$.pipe(
      startWith(undefined),
      switchMap(() =>
        this.api.getSummary().pipe(
          map((summary): SummaryState => ({ summary, error: false })),
          catchError(() => of<SummaryState>({ summary: null, error: true }))
        )
      )
    )
  );

  readonly total = computed(() => this.state()?.summary?.byStatus.reduce((sum, item) => sum + item.count, 0) ?? 0);

  refresh(): void {
    this.refresh$.next();
  }
}
