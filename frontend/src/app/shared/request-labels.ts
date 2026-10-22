import { Pipe, PipeTransform } from '@angular/core';
import { RequestPriority, RequestStatus } from '../core/models/service-request.models';

const STATUS_LABELS: Record<RequestStatus, string> = {
  New: 'חדשה',
  InProgress: 'בטיפול',
  Waiting: 'ממתינה',
  Completed: 'הושלמה'
};

const PRIORITY_LABELS: Record<RequestPriority, string> = {
  Low: 'נמוכה',
  Medium: 'בינונית',
  High: 'גבוהה'
};

@Pipe({ name: 'statusLabel', standalone: true })
export class StatusLabelPipe implements PipeTransform {
  transform(status: RequestStatus): string {
    return STATUS_LABELS[status];
  }
}

@Pipe({ name: 'priorityLabel', standalone: true })
export class PriorityLabelPipe implements PipeTransform {
  transform(priority: RequestPriority): string {
    return PRIORITY_LABELS[priority];
  }
}
