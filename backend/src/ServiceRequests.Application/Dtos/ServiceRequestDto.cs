using ServiceRequests.Domain.Enums;

namespace ServiceRequests.Application.Dtos;

public record ServiceRequestDto(
    int Id,
    string Title,
    string OrganizationName,
    RequestStatus Status,
    RequestPriority Priority,
    string? AssignedTo,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    string RowVersion);
