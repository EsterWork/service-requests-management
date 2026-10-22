using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using ServiceRequests.Application.Dtos;
using ServiceRequests.Application.Interfaces;
using ServiceRequests.Application.Mapping;
using ServiceRequests.Domain.Entities;

namespace ServiceRequests.Application.Services;

public class RequestService(IApplicationDbContext dbContext) : IRequestService
{
    // Keys match RequestSortFields; the validator rejects any other sortBy value before it gets here.
    private static readonly Dictionary<string, Func<IQueryable<ServiceRequest>, bool, IOrderedQueryable<ServiceRequest>>> SortStrategies =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [RequestSortFields.CreatedAt] = (query, descending) => OrderBy(query, request => request.CreatedAt, descending),
            [RequestSortFields.UpdatedAt] = (query, descending) => OrderBy(query, request => request.UpdatedAt, descending),
            [RequestSortFields.Priority] = (query, descending) => OrderBy(query, request => request.Priority, descending),
            [RequestSortFields.Title] = (query, descending) => OrderBy(query, request => request.Title, descending)
        };
    public async Task<PagedResult<ServiceRequestDto>> GetRequestsAsync(RequestQueryParameters parameters, CancellationToken cancellationToken)
    {
        // The query stays IQueryable until ToListAsync: filtering, sorting and paging all run in SQL.
        var query = ApplyFilters(dbContext.ServiceRequests.AsNoTracking(), parameters);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await ApplySorting(query, parameters)
            .Skip((parameters.Page - 1) * parameters.PageSize)
            .Take(parameters.PageSize)
            .Select(ServiceRequestMapping.ToDto)
            .ToListAsync(cancellationToken);

        return new PagedResult<ServiceRequestDto>(items, parameters.Page, parameters.PageSize, totalCount);
    }

    private static IQueryable<ServiceRequest> ApplyFilters(IQueryable<ServiceRequest> query, RequestQueryParameters parameters)
    {
        if (parameters.Status.HasValue)
        {
            query = query.Where(request => request.Status == parameters.Status);
        }

        if (parameters.Priority.HasValue)
        {
            query = query.Where(request => request.Priority == parameters.Priority);
        }

        if (!string.IsNullOrWhiteSpace(parameters.OrganizationName))
        {
            query = query.Where(request => request.OrganizationName == parameters.OrganizationName);
        }

        if (!string.IsNullOrWhiteSpace(parameters.AssignedTo))
        {
            query = query.Where(request => request.AssignedTo == parameters.AssignedTo);
        }

        if (parameters.CreatedFrom.HasValue)
        {
            var createdFrom = parameters.CreatedFrom.Value.UtcDateTime;
            query = query.Where(request => request.CreatedAt >= createdFrom);
        }

        if (parameters.CreatedTo.HasValue)
        {
            var createdTo = parameters.CreatedTo.Value.UtcDateTime;
            query = query.Where(request => request.CreatedAt <= createdTo);
        }

        if (!string.IsNullOrWhiteSpace(parameters.Search))
        {
            // Prefix search (LIKE 'term%') can seek the Title / OrganizationName indexes; '%term%' would scan the table.
            var search = parameters.Search.Trim();
            query = query.Where(request => request.Title.StartsWith(search) || request.OrganizationName.StartsWith(search));
        }

        return query;
    }

    private static IQueryable<ServiceRequest> ApplySorting(IQueryable<ServiceRequest> query, RequestQueryParameters parameters)
    {
        var descending = string.Equals(parameters.SortDirection, SortDirections.Descending, StringComparison.OrdinalIgnoreCase);
        return SortStrategies[parameters.SortBy](query, descending);
    }

    // Id as a tie-breaker gives a stable order, so rows don't repeat or disappear between pages.
    private static IOrderedQueryable<ServiceRequest> OrderBy<TKey>(
        IQueryable<ServiceRequest> query, Expression<Func<ServiceRequest, TKey>> keySelector, bool descending) =>
        descending
            ? query.OrderByDescending(keySelector).ThenByDescending(request => request.Id)
            : query.OrderBy(keySelector).ThenBy(request => request.Id);
}
