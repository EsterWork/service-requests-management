using ServiceRequests.Application.Dtos;

namespace ServiceRequests.Application.Services;

public interface IRequestService
{
    Task<PagedResult<ServiceRequestDto>> GetRequestsAsync(RequestQueryParameters parameters, CancellationToken cancellationToken);
}
