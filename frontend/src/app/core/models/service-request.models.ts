export type RequestStatus = 'New' | 'InProgress' | 'Waiting' | 'Completed';
export type RequestPriority = 'Low' | 'Medium' | 'High';

export const REQUEST_STATUSES: readonly RequestStatus[] = ['New', 'InProgress', 'Waiting', 'Completed'];
export const REQUEST_PRIORITIES: readonly RequestPriority[] = ['Low', 'Medium', 'High'];

/**
 * Mirrors the server's StatusTransitionValidator so the UI only offers valid choices.
 * The server remains the authority and rejects any other transition with 400.
 */
export const ALLOWED_TRANSITIONS: Readonly<Record<RequestStatus, readonly RequestStatus[]>> = {
  New: ['InProgress'],
  InProgress: ['Waiting', 'Completed'],
  Waiting: ['InProgress'],
  Completed: []
};

export interface ServiceRequest {
  id: number;
  title: string;
  organizationName: string;
  status: RequestStatus;
  priority: RequestPriority;
  assignedTo: string | null;
  createdAt: string;
  updatedAt: string;
  rowVersion: string;
}

export interface UpdateStatusRequest {
  newStatus: RequestStatus;
  rowVersion: string;
  changedBy: string;
}

export interface RequestStatusHistory {
  id: number;
  requestId: number;
  previousStatus: RequestStatus;
  newStatus: RequestStatus;
  changedAt: string;
  changedBy: string;
}

export interface RequestSummary {
  byStatus: { status: RequestStatus; count: number }[];
  byPriority: { priority: RequestPriority; count: number }[];
}

export interface PagedResult<T> {
  items: T[];
  page: number;
  pageSize: number;
  totalCount: number;
  totalPages: number;
}

/** Sort fields accepted by the API (server-side whitelist). */
export type SortField = 'createdAt' | 'updatedAt' | 'priority' | 'title';
export type SortDirection = 'asc' | 'desc';

export interface RequestFilters {
  search: string;
  status: RequestStatus | null;
  priority: RequestPriority | null;
  organizationName: string;
  assignedTo: string;
  /** ISO 8601 (UTC) start of the selected local day. */
  createdFrom: string | null;
  /** ISO 8601 (UTC) end of the selected local day. */
  createdTo: string | null;
}

export interface RequestQuery extends RequestFilters {
  page: number;
  pageSize: number;
  sortBy: SortField;
  sortDirection: SortDirection;
}

export const EMPTY_FILTERS: RequestFilters = {
  search: '',
  status: null,
  priority: null,
  organizationName: '',
  assignedTo: '',
  createdFrom: null,
  createdTo: null
};

export const DEFAULT_QUERY: RequestQuery = {
  ...EMPTY_FILTERS,
  page: 1,
  pageSize: 20,
  sortBy: 'createdAt',
  sortDirection: 'desc'
};
