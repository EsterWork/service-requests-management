using ServiceRequests.Application.Dtos;

namespace ServiceRequests.Application.Services;

public interface IRequestStatusService
{
    Task<StatusUpdateResult> UpdateStatusAsync(int id, UpdateStatusRequest request, CancellationToken cancellationToken);
}
