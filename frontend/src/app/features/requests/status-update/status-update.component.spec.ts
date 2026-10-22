import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { ServiceRequest } from '../../../core/models/service-request.models';
import { StatusUpdateComponent } from './status-update.component';

const REQUEST: ServiceRequest = {
  id: 7,
  title: 'תיקון תלוש שכר - יוני 2025',
  organizationName: 'גליל לוגיסטיקה בע"מ',
  status: 'InProgress',
  priority: 'High',
  assignedTo: 'דנה כהן',
  createdAt: '2025-06-01T08:00:00Z',
  updatedAt: '2025-06-02T08:00:00Z',
  rowVersion: 'AAAAAAAAB9E='
};

describe('StatusUpdateComponent', () => {
  let component: StatusUpdateComponent;
  let http: HttpTestingController;
  let dialogRef: jasmine.SpyObj<MatDialogRef<StatusUpdateComponent>>;

  beforeEach(() => {
    dialogRef = jasmine.createSpyObj<MatDialogRef<StatusUpdateComponent>>('MatDialogRef', ['close']);
    TestBed.configureTestingModule({
      imports: [StatusUpdateComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideNoopAnimations(),
        { provide: MAT_DIALOG_DATA, useValue: { request: REQUEST } },
        { provide: MatDialogRef, useValue: dialogRef }
      ]
    });
    http = TestBed.inject(HttpTestingController);
    component = TestBed.createComponent(StatusUpdateComponent).componentInstance;
    component.form.controls.changedBy.setValue('דנה כהן');
  });

  function saveAndRespond(body: object, status = 200): void {
    component.save();
    const request = http.expectOne('/api/requests/7/status');
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ newStatus: 'Waiting', rowVersion: REQUEST.rowVersion, changedBy: 'דנה כהן' });
    request.flush(body, { status, statusText: String(status) });
  }

  it('offers only the transitions allowed from the current status', () => {
    expect(component.nextStatuses).toEqual(['Waiting', 'Completed']);
  });

  it('sends the rowVersion the user saw and closes as "updated" on success', () => {
    saveAndRespond({ ...REQUEST, status: 'Waiting', rowVersion: 'AAAAAAAAB9I=' });

    expect(dialogRef.close).toHaveBeenCalledWith('updated');
  });

  it('closes as "conflict" on 409 so the list reloads and the user sees the current data', () => {
    saveAndRespond({ title: 'Conflict' }, 409);

    expect(dialogRef.close).toHaveBeenCalledWith('conflict');
  });

  it('stays open with a message when the server rejects the transition (400)', () => {
    saveAndRespond({ title: 'One or more validation errors occurred.' }, 400);

    expect(dialogRef.close).not.toHaveBeenCalled();
    expect(component.errorMessage()).toBeTruthy();
    expect(component.saving()).toBeFalse();
  });
});
