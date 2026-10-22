namespace ServiceRequests.Application.Dtos;

public record BulkUpdateStatusResult(IReadOnlyList<BulkItemResult> Results, BulkUpdateSummary Summary);

/// <summary>Per-item outcome. RowVersion is the new value after a successful update, so the client can continue editing.</summary>
public record BulkItemResult(int Id, StatusUpdateOutcome Outcome, string? RowVersion, string? Error);

public record BulkUpdateSummary(int Total, int Succeeded, int NotFound, int InvalidTransition, int Conflict);
