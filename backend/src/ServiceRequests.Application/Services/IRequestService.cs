using ServiceRequests.Application.Dtos;

namespace ServiceRequests.Application.Services;

public interface IRequestService
{
    Task<PagedResult<ServiceRequestDto>> GetRequestsAsync(RequestQueryParameters parameters, CancellationToken cancellationToken);

    /// <summary>Returns the status history newest first, or null when the request does not exist.</summary>
    Task<IReadOnlyList<RequestStatusHistoryDto>?> GetHistoryAsync(int requestId, CancellationToken cancellationToken);
}
