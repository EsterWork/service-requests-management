import { Component, inject, signal } from '@angular/core';
import { HttpErrorResponse, HttpStatusCode } from '@angular/common/http';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MAT_DIALOG_DATA, MatDialogModule, MatDialogRef } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatSelectModule } from '@angular/material/select';
import { ALLOWED_TRANSITIONS, RequestStatus, ServiceRequest } from '../../../core/models/service-request.models';
import { RequestsApiService } from '../../../core/services/requests-api.service';
import { StatusLabelPipe } from '../../../shared/request-labels';

export interface StatusUpdateDialogData {
  request: ServiceRequest;
}

/** How the dialog ended; conflict and notFound mean the shown data is outdated and must be reloaded. */
export type StatusUpdateOutcome = 'updated' | 'conflict' | 'notFound';

const CHANGED_BY_STORAGE_KEY = 'serviceRequests.changedBy';

@Component({
  selector: 'app-status-update',
  standalone: true,
  imports: [ReactiveFormsModule, MatButtonModule, MatDialogModule, MatFormFieldModule, MatInputModule, MatSelectModule, StatusLabelPipe],
  templateUrl: './status-update.component.html',
  styleUrl: './status-update.component.scss'
})
export class StatusUpdateComponent {
  private readonly api = inject(RequestsApiService);
  private readonly dialogRef = inject<MatDialogRef<StatusUpdateComponent, StatusUpdateOutcome>>(MatDialogRef);

  readonly request = inject<StatusUpdateDialogData>(MAT_DIALOG_DATA).request;
  readonly nextStatuses = ALLOWED_TRANSITIONS[this.request.status];

  readonly saving = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly form = new FormGroup({
    newStatus: new FormControl<RequestStatus | null>(this.nextStatuses[0] ?? null, Validators.required),
    changedBy: new FormControl(readChangedBy(), {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(100)]
    })
  });

  save(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    const changedBy = this.form.controls.changedBy.value.trim();
    rememberChangedBy(changedBy);
    this.saving.set(true);
    this.errorMessage.set(null);
    this.dialogRef.disableClose = true;

    // The rowVersion the user saw is sent back, so the server rejects the update if someone else changed it meanwhile.
    this.api
      .updateStatus(this.request.id, {
        newStatus: this.form.controls.newStatus.value!,
        rowVersion: this.request.rowVersion,
        changedBy
      })
      .subscribe({
        next: () => this.dialogRef.close('updated'),
        error: (error: HttpErrorResponse) => this.handleError(error)
      });
  }

  private handleError(error: HttpErrorResponse): void {
    if (error.status === HttpStatusCode.Conflict) {
      this.dialogRef.close('conflict');
      return;
    }
    if (error.status === HttpStatusCode.NotFound) {
      this.dialogRef.close('notFound');
      return;
    }

    this.saving.set(false);
    this.dialogRef.disableClose = false;
    this.errorMessage.set(
      error.status === HttpStatusCode.BadRequest
        ? 'המעבר לסטטוס שנבחר אינו מותר או שהנתונים אינם תקינים.'
        : 'אירעה שגיאה בעדכון. נסה שוב.'
    );
  }
}

// Remembers the user's name between updates (no authentication in this system). Storage may be unavailable.
function readChangedBy(): string {
  try {
    return localStorage.getItem(CHANGED_BY_STORAGE_KEY) ?? '';
  } catch {
    return '';
  }
}

function rememberChangedBy(changedBy: string): void {
  try {
    localStorage.setItem(CHANGED_BY_STORAGE_KEY, changedBy);
  } catch {
    // Not remembering the name is acceptable.
  }
}
