import { DEFAULT_QUERY, RequestQuery } from '../../core/models/service-request.models';
import { readQueryFromUrl, toUrlQueryString } from './request-query-url';

describe('request query URL', () => {
  it('writes nothing for the default query', () => {
    expect(toUrlQueryString(DEFAULT_QUERY)).toBe('');
  });

  it('restores the same query it wrote', () => {
    const query: RequestQuery = {
      ...DEFAULT_QUERY,
      search: 'גליל',
      status: 'InProgress',
      priority: 'High',
      createdFrom: '2025-01-01T00:00:00.000Z',
      page: 4,
      pageSize: 50,
      sortBy: 'title',
      sortDirection: 'asc'
    };

    expect(readQueryFromUrl(toUrlQueryString(query))).toEqual(query);
  });

  it('falls back to defaults for invalid values instead of sending a broken request', () => {
    const query = readQueryFromUrl('?status=Closed&page=-2&pageSize=7&sortBy=status&createdFrom=yesterday');

    expect(query).toEqual(DEFAULT_QUERY);
  });
});
