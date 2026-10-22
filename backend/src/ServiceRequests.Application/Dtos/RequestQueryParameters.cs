using ServiceRequests.Domain.Enums;

namespace ServiceRequests.Application.Dtos;

public class RequestQueryParameters
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = DefaultPageSize;
    public RequestStatus? Status { get; set; }
    public RequestPriority? Priority { get; set; }
    public string? OrganizationName { get; set; }
    public string? AssignedTo { get; set; }

    // DateTimeOffset keeps the client's offset, so "…Z" and "…+03:00" are both converted to UTC correctly.
    public DateTimeOffset? CreatedFrom { get; set; }
    public DateTimeOffset? CreatedTo { get; set; }

    /// <summary>Prefix search on Title or OrganizationName.</summary>
    public string? Search { get; set; }

    public string SortBy { get; set; } = RequestSortFields.CreatedAt;
    public string SortDirection { get; set; } = SortDirections.Descending;
}
