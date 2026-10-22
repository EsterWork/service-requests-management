using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using ServiceRequests.Application.Dtos;
using ServiceRequests.Application.Interfaces;
using ServiceRequests.Domain.Enums;

namespace ServiceRequests.Application.Services;

/// <summary>
/// The summary aggregates the whole table on every call and is identical for all users,
/// so it is cached under a single key and invalidated whenever a status changes.
/// </summary>
public class RequestSummaryService(IApplicationDbContext dbContext, IMemoryCache cache, ILogger<RequestSummaryService> logger)
    : IRequestSummaryService
{
    private const string CacheKey = "requests:summary";
    private static readonly TimeSpan CacheDuration = TimeSpan.FromSeconds(60);

    public async Task<RequestSummaryDto> GetSummaryAsync(CancellationToken cancellationToken) =>
        (await cache.GetOrCreateAsync(CacheKey, entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = CacheDuration;
            return LoadSummaryAsync(cancellationToken);
        }))!;

    public void InvalidateCache()
    {
        cache.Remove(CacheKey);
        logger.LogInformation("Summary cache invalidated");
    }

    private async Task<RequestSummaryDto> LoadSummaryAsync(CancellationToken cancellationToken)
    {
        logger.LogInformation("Summary cache miss, loading from database");

        var statusCounts = await dbContext.ServiceRequests
            .GroupBy(request => request.Status)
            .Select(group => new { Status = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Status, item => item.Count, cancellationToken);

        var priorityCounts = await dbContext.ServiceRequests
            .GroupBy(request => request.Priority)
            .Select(group => new { Priority = group.Key, Count = group.Count() })
            .ToDictionaryAsync(item => item.Priority, item => item.Count, cancellationToken);

        // Every enum value is returned (with 0 when missing) so clients always get a complete, stable list.
        return new RequestSummaryDto(
            Enum.GetValues<RequestStatus>().Select(status => new StatusCount(status, statusCounts.GetValueOrDefault(status))).ToList(),
            Enum.GetValues<RequestPriority>().Select(priority => new PriorityCount(priority, priorityCounts.GetValueOrDefault(priority))).ToList());
    }
}
