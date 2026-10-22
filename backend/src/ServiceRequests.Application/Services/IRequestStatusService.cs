using ServiceRequests.Application.Dtos;

namespace ServiceRequests.Application.Services;

public interface IRequestStatusService
{
    Task<StatusUpdateResult> UpdateStatusAsync(int id, UpdateStatusRequest request, CancellationToken cancellationToken);

    /// <summary>Partial success: every item is processed and saved independently.</summary>
    Task<BulkUpdateStatusResult> BulkUpdateStatusAsync(BulkUpdateStatusRequest request, CancellationToken cancellationToken);
}
