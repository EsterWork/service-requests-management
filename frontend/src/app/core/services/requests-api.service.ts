import { Injectable, inject } from '@angular/core';
import { HttpClient, HttpParams } from '@angular/common/http';
import { Observable } from 'rxjs';
import { PagedResult, RequestQuery, ServiceRequest } from '../models/service-request.models';

/** The only place that talks to the requests API; components never use HttpClient directly. */
@Injectable({ providedIn: 'root' })
export class RequestsApiService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/requests';

  getRequests(query: RequestQuery): Observable<PagedResult<ServiceRequest>> {
    return this.http.get<PagedResult<ServiceRequest>>(this.baseUrl, { params: toHttpParams(query) });
  }
}

/** Sends only parameters that have a value, so the server applies its defaults for the rest. */
function toHttpParams(query: RequestQuery): HttpParams {
  let params = new HttpParams();
  for (const [key, value] of Object.entries(query)) {
    if (value !== null && value !== '') {
      params = params.set(key, String(value));
    }
  }
  return params;
}
