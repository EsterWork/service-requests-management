import { HttpParams } from '@angular/common/http';
import {
  DEFAULT_QUERY,
  REQUEST_PRIORITIES,
  REQUEST_STATUSES,
  RequestPriority,
  RequestQuery,
  RequestStatus,
  SortDirection,
  SortField
} from '../../core/models/service-request.models';

const SORT_FIELDS: readonly SortField[] = ['createdAt', 'updatedAt', 'priority', 'title'];
const SORT_DIRECTIONS: readonly SortDirection[] = ['asc', 'desc'];
export const PAGE_SIZE_OPTIONS: readonly number[] = [10, 20, 50, 100];

/**
 * Reads the list state from the URL query string, so a refresh or a shared link shows the same view.
 * Unknown or invalid values fall back to the defaults instead of producing a broken request.
 */
export function readQueryFromUrl(queryString: string): RequestQuery {
  const params = new HttpParams({ fromString: queryString.replace(/^\?/, '') });
  const text = (name: string) => params.get(name)?.trim() ?? '';
  const oneOf = <T extends string>(name: string, allowed: readonly T[]): T | null =>
    allowed.find(value => value === params.get(name)) ?? null;
  const isoDate = (name: string) => (Number.isNaN(Date.parse(text(name))) ? null : text(name) || null);
  const page = Number(params.get('page'));
  const pageSize = Number(params.get('pageSize'));

  return {
    search: text('search'),
    status: oneOf<RequestStatus>('status', REQUEST_STATUSES),
    priority: oneOf<RequestPriority>('priority', REQUEST_PRIORITIES),
    organizationName: text('organizationName'),
    assignedTo: text('assignedTo'),
    createdFrom: isoDate('createdFrom'),
    createdTo: isoDate('createdTo'),
    page: Number.isInteger(page) && page >= 1 ? page : DEFAULT_QUERY.page,
    pageSize: PAGE_SIZE_OPTIONS.includes(pageSize) ? pageSize : DEFAULT_QUERY.pageSize,
    sortBy: oneOf<SortField>('sortBy', SORT_FIELDS) ?? DEFAULT_QUERY.sortBy,
    sortDirection: oneOf<SortDirection>('sortDirection', SORT_DIRECTIONS) ?? DEFAULT_QUERY.sortDirection
  };
}

/** Writes only values that differ from the defaults, to keep the URL short. */
export function toUrlQueryString(query: RequestQuery): string {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(query)) {
    if (value !== null && value !== '' && value !== DEFAULT_QUERY[key as keyof RequestQuery]) {
      params = params.set(key, String(value));
    }
  }
  return params.toString();
}
