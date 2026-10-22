using System.Linq.Expressions;
using ServiceRequests.Application.Dtos;
using ServiceRequests.Domain.Entities;

namespace ServiceRequests.Application.Mapping;

public static class ServiceRequestMapping
{
    /// <summary>
    /// Expression (not a method) so EF translates it into the SELECT and reads only the DTO columns.
    /// </summary>
    public static readonly Expression<Func<ServiceRequest, ServiceRequestDto>> ToDto = request => new ServiceRequestDto(
        request.Id,
        request.Title,
        request.OrganizationName,
        request.Status,
        request.Priority,
        request.AssignedTo,
        request.CreatedAt,
        request.UpdatedAt,
        Convert.ToBase64String(request.RowVersion));

    private static readonly Func<ServiceRequest, ServiceRequestDto> ToDtoCompiled = ToDto.Compile();

    /// <summary>Maps an entity already in memory (e.g. after an update) using the same definition.</summary>
    public static ServiceRequestDto ToDtoFromEntity(this ServiceRequest request) => ToDtoCompiled(request);

    public static readonly Expression<Func<RequestStatusHistory, RequestStatusHistoryDto>> ToHistoryDto = history =>
        new RequestStatusHistoryDto(
            history.Id,
            history.RequestId,
            history.PreviousStatus,
            history.NewStatus,
            history.ChangedAt,
            history.ChangedBy);
}
