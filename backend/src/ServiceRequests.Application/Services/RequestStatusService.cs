using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceRequests.Application.Dtos;
using ServiceRequests.Application.Interfaces;
using ServiceRequests.Application.Mapping;
using ServiceRequests.Domain;

namespace ServiceRequests.Application.Services;

public class RequestStatusService(
    IApplicationDbContext dbContext,
    IRequestSummaryService summaryService,
    TimeProvider timeProvider,
    ILogger<RequestStatusService> logger) : IRequestStatusService
{
    public async Task<StatusUpdateResult> UpdateStatusAsync(int id, UpdateStatusRequest request, CancellationToken cancellationToken)
    {
        var serviceRequest = await dbContext.ServiceRequests.FirstOrDefaultAsync(entity => entity.Id == id, cancellationToken);
        if (serviceRequest is null)
        {
            return StatusUpdateResult.NotFound(id);
        }

        var clientRowVersion = Convert.FromBase64String(request.RowVersion);

        // A stale rowVersion means the client decided based on old data, so the transition is not even evaluated.
        if (!serviceRequest.RowVersion.SequenceEqual(clientRowVersion))
        {
            logger.LogWarning("Concurrency conflict on request {RequestId}: stale rowVersion", id);
            return StatusUpdateResult.Conflict(id);
        }

        var newStatus = request.NewStatus!.Value;
        if (!StatusTransitionValidator.IsAllowed(serviceRequest.Status, newStatus))
        {
            return StatusUpdateResult.InvalidTransition($"Transition from {serviceRequest.Status} to {newStatus} is not allowed.");
        }

        var previousStatus = serviceRequest.Status;

        // EF adds "WHERE RowVersion = @original" to the UPDATE, which also catches a change made
        // between the read above and this save. The audit row is inserted in the same transaction.
        dbContext.Entry(serviceRequest).Property(entity => entity.RowVersion).OriginalValue = clientRowVersion;
        serviceRequest.ChangeStatus(newStatus, request.ChangedBy, timeProvider.GetUtcNow().UtcDateTime);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict on request {RequestId}: modified during update", id);
            return StatusUpdateResult.Conflict(id);
        }

        summaryService.InvalidateCache();
        logger.LogInformation("Request {RequestId} status changed from {PreviousStatus} to {NewStatus}", id, previousStatus, newStatus);

        return StatusUpdateResult.Succeeded(serviceRequest.ToDtoFromEntity());
    }
}
