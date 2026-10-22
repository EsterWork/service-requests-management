using ServiceRequests.Application.Dtos;

namespace ServiceRequests.Application.Services;

public interface IRequestSummaryService
{
    Task<RequestSummaryDto> GetSummaryAsync(CancellationToken cancellationToken);

    /// <summary>Must be called after every committed status change.</summary>
    void InvalidateCache();
}
