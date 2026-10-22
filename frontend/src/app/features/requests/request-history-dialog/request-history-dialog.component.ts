import { Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { DatePipe } from '@angular/common';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule } from '@angular/material/dialog';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressSpinnerModule } from '@angular/material/progress-spinner';
import { catchError, map, of } from 'rxjs';
import { RequestStatusHistory, ServiceRequest } from '../../../core/models/service-request.models';
import { RequestsApiService } from '../../../core/services/requests-api.service';
import { StatusLabelPipe } from '../../../shared/request-labels';

export interface RequestHistoryDialogData {
  request: ServiceRequest;
}

interface HistoryState {
  history: RequestStatusHistory[] | null;
  error: boolean;
}

@Component({
  selector: 'app-request-history-dialog',
  standalone: true,
  imports: [DatePipe, MatButtonModule, MatDialogModule, MatIconModule, MatProgressSpinnerModule, StatusLabelPipe],
  templateUrl: './request-history-dialog.component.html',
  styleUrl: './request-history-dialog.component.scss'
})
export class RequestHistoryDialogComponent {
  private readonly api = inject(RequestsApiService);

  readonly request = inject<RequestHistoryDialogData>(MAT_DIALOG_DATA).request;

  readonly state = toSignal(
    this.api.getHistory(this.request.id).pipe(
      map((history): HistoryState => ({ history, error: false })),
      catchError(() => of<HistoryState>({ history: null, error: true }))
    )
  );
}
