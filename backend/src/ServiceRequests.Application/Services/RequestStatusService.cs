using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceRequests.Application.Dtos;
using ServiceRequests.Application.Interfaces;
using ServiceRequests.Application.Mapping;
using ServiceRequests.Domain;
using ServiceRequests.Domain.Entities;
using ServiceRequests.Domain.Enums;

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

        var result = await ApplyStatusChangeAsync(serviceRequest, Convert.FromBase64String(request.RowVersion),
            request.NewStatus!.Value, request.ChangedBy, cancellationToken);

        if (result.Outcome == StatusUpdateOutcome.Succeeded)
        {
            summaryService.InvalidateCache();
        }

        return result;
    }

    public async Task<BulkUpdateStatusResult> BulkUpdateStatusAsync(BulkUpdateStatusRequest request, CancellationToken cancellationToken)
    {
        var ids = request.Items.Select(item => item.Id).ToList();

        // One query for all items (WHERE Id IN (...)); entities are attached one at a time below.
        var serviceRequests = await dbContext.ServiceRequests
            .AsNoTracking()
            .Where(entity => ids.Contains(entity.Id))
            .ToDictionaryAsync(entity => entity.Id, cancellationToken);

        var results = new List<BulkItemResult>(request.Items.Count);
        foreach (var item in request.Items)
        {
            var result = await UpdateBulkItemAsync(serviceRequests.GetValueOrDefault(item.Id), item, request, cancellationToken);
            results.Add(new BulkItemResult(item.Id, result.Outcome, result.Request?.RowVersion, result.Error));
        }

        var summary = Summarize(results);
        if (summary.Succeeded > 0)
        {
            summaryService.InvalidateCache();
        }

        logger.LogInformation(
            "Bulk status update to {NewStatus}: {Total} items, {Succeeded} succeeded, {NotFound} not found, {InvalidTransition} invalid transition, {Conflict} conflict",
            request.NewStatus, summary.Total, summary.Succeeded, summary.NotFound, summary.InvalidTransition, summary.Conflict);

        return new BulkUpdateStatusResult(results, summary);
    }

    private async Task<StatusUpdateResult> UpdateBulkItemAsync(
        ServiceRequest? serviceRequest, BulkUpdateStatusItem item, BulkUpdateStatusRequest request, CancellationToken cancellationToken)
    {
        if (serviceRequest is null)
        {
            return StatusUpdateResult.NotFound(item.Id);
        }

        dbContext.ServiceRequests.Attach(serviceRequest);
        try
        {
            return await ApplyStatusChangeAsync(serviceRequest, Convert.FromBase64String(item.RowVersion),
                request.NewStatus!.Value, request.ChangedBy, cancellationToken);
        }
        finally
        {
            // Each item is its own unit of work: a failed item must not be retried by the next SaveChanges.
            dbContext.ChangeTracker.Clear();
        }
    }

    /// <summary>
    /// Shared by single and bulk updates. Expects a tracked entity; saves the status change and its audit row
    /// in one SaveChanges (one transaction), guarded by the client's rowVersion.
    /// </summary>
    private async Task<StatusUpdateResult> ApplyStatusChangeAsync(
        ServiceRequest serviceRequest, byte[] clientRowVersion, RequestStatus newStatus, string changedBy, CancellationToken cancellationToken)
    {
        // A stale rowVersion means the client decided based on old data, so the transition is not even evaluated.
        if (!serviceRequest.RowVersion.SequenceEqual(clientRowVersion))
        {
            logger.LogWarning("Concurrency conflict on request {RequestId}: stale rowVersion", serviceRequest.Id);
            return StatusUpdateResult.Conflict(serviceRequest.Id);
        }

        var previousStatus = serviceRequest.Status;
        if (!StatusTransitionValidator.IsAllowed(previousStatus, newStatus))
        {
            return StatusUpdateResult.InvalidTransition($"Transition from {previousStatus} to {newStatus} is not allowed.");
        }

        // EF adds "WHERE RowVersion = @original" to the UPDATE, which also catches a change made
        // between the read and this save.
        dbContext.Entry(serviceRequest).Property(entity => entity.RowVersion).OriginalValue = clientRowVersion;
        serviceRequest.ChangeStatus(newStatus, changedBy, timeProvider.GetUtcNow().UtcDateTime);

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException)
        {
            logger.LogWarning("Concurrency conflict on request {RequestId}: modified during update", serviceRequest.Id);
            return StatusUpdateResult.Conflict(serviceRequest.Id);
        }

        logger.LogInformation("Request {RequestId} status changed from {PreviousStatus} to {NewStatus}",
            serviceRequest.Id, previousStatus, newStatus);

        return StatusUpdateResult.Succeeded(serviceRequest.ToDtoFromEntity());
    }

    private static BulkUpdateSummary Summarize(IReadOnlyCollection<BulkItemResult> results)
    {
        int Count(StatusUpdateOutcome outcome) => results.Count(result => result.Outcome == outcome);

        return new BulkUpdateSummary(
            results.Count,
            Count(StatusUpdateOutcome.Succeeded),
            Count(StatusUpdateOutcome.NotFound),
            Count(StatusUpdateOutcome.InvalidTransition),
            Count(StatusUpdateOutcome.Conflict));
    }
}
