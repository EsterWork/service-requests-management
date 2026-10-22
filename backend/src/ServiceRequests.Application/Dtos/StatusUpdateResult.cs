namespace ServiceRequests.Application.Dtos;

public record StatusUpdateResult(StatusUpdateOutcome Outcome, ServiceRequestDto? Request = null, string? Error = null)
{
    public static StatusUpdateResult Succeeded(ServiceRequestDto request) => new(StatusUpdateOutcome.Succeeded, request);
    public static StatusUpdateResult NotFound(int id) => new(StatusUpdateOutcome.NotFound, Error: $"Request {id} was not found.");
    public static StatusUpdateResult InvalidTransition(string error) => new(StatusUpdateOutcome.InvalidTransition, Error: error);
    public static StatusUpdateResult Conflict(int id) =>
        new(StatusUpdateOutcome.Conflict, Error: $"Request {id} was modified by another user. Reload it and try again.");
}
