import { TestBed, fakeAsync, tick } from '@angular/core/testing';
import { provideNoopAnimations } from '@angular/platform-browser/animations';
import { RequestFilters } from '../../../core/models/service-request.models';
import { RequestFiltersComponent, TEXT_DEBOUNCE_MS } from './request-filters.component';

describe('RequestFiltersComponent', () => {
  let component: RequestFiltersComponent;
  let emitted: RequestFilters[];

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [RequestFiltersComponent],
      providers: [provideNoopAnimations()]
    });
    const fixture = TestBed.createComponent(RequestFiltersComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();

    emitted = [];
    component.filtersChange.subscribe(filters => emitted.push(filters));
  });

  it('sends a single search only after the user stops typing for 300ms', fakeAsync(() => {
    for (const text of ['ג', 'גל', 'גלי', 'גליל']) {
      component.form.controls.search.setValue(text);
      tick(100);
    }
    expect(emitted).toEqual([]);

    tick(TEXT_DEBOUNCE_MS);

    expect(emitted.map(filters => filters.search)).toEqual(['גליל']);
  }));

  it('does not send the same filters twice', fakeAsync(() => {
    component.form.controls.search.setValue('גליל');
    tick(TEXT_DEBOUNCE_MS);
    component.form.controls.search.setValue('גליל ');
    tick(TEXT_DEBOUNCE_MS);

    expect(emitted.length).toBe(1);
  }));

  it('applies a status choice immediately, without the typing debounce', fakeAsync(() => {
    component.form.controls.status.setValue('Waiting');
    tick(0);

    expect(emitted.map(filters => filters.status)).toEqual(['Waiting']);
  }));
});
