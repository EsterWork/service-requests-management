import { ComponentFixture, TestBed } from '@angular/core/testing';
import { Location } from '@angular/common';
import { SpyLocation, provideLocationMocks } from '@angular/common/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, TestRequest, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { EMPTY_FILTERS, PagedResult, ServiceRequest } from '../../../core/models/service-request.models';
import { RequestListComponent } from './request-list.component';

const EMPTY_PAGE: PagedResult<ServiceRequest> = { items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 };

describe('RequestListComponent', () => {
  let fixture: ComponentFixture<RequestListComponent>;
  let http: HttpTestingController;
  let location: SpyLocation;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RequestListComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideNoopAnimations(), provideLocationMocks()]
    });
    http = TestBed.inject(HttpTestingController);
    location = TestBed.inject(Location) as SpyLocation;
  });

  function create(): void {
    fixture = TestBed.createComponent(RequestListComponent);
    fixture.detectChanges();
  }

  function expectListRequest(): TestRequest {
    return http.expectOne(request => request.url === '/api/requests');
  }

  it('restores filters, sort and page from the URL and requests them from the server', () => {
    location.setInitialPath('/?status=Waiting&sortBy=title&sortDirection=asc&page=3');

    create();
    const params = expectListRequest().request.params;

    expect(params.get('status')).toBe('Waiting');
    expect(params.get('sortBy')).toBe('title');
    expect(params.get('sortDirection')).toBe('asc');
    expect(params.get('page')).toBe('3');
  });

  it('cancels the in-flight request when a newer query arrives and shows only the latest result', () => {
    spyOn(location, 'replaceState').and.callThrough();
    create();
    const first = expectListRequest();

    fixture.componentInstance.onFiltersChange({ ...EMPTY_FILTERS, search: 'גליל' });
    fixture.detectChanges();
    const second = expectListRequest();

    expect(first.cancelled).toBeTrue();
    expect(second.request.params.get('search')).toBe('גליל');

    second.flush({ ...EMPTY_PAGE, totalCount: 4015, totalPages: 201 });
    expect(fixture.componentInstance.state().result?.totalCount).toBe(4015);
    expect(location.replaceState).toHaveBeenCalledWith(jasmine.any(String), jasmine.stringContaining('search='));
  });

  it('shows the error state when the API fails, and retries the same query', () => {
    create();
    expectListRequest().flush(null, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(fixture.componentInstance.state().error).toBeTrue();
    expect(fixture.nativeElement.textContent).toContain('אירעה שגיאה בטעינת הפניות');

    fixture.componentInstance.reload();
    fixture.detectChanges();
    expectListRequest().flush(EMPTY_PAGE);
    fixture.detectChanges();

    expect(fixture.componentInstance.state().error).toBeFalse();
    expect(fixture.nativeElement.textContent).toContain('לא נמצאו פניות');
  });
});
