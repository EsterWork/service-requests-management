using ServiceRequests.Domain.Enums;

namespace ServiceRequests.Domain;

/// <summary>
/// Single source of truth for the allowed status workflow:
/// New → InProgress → (Waiting ⇄ InProgress) → Completed. Completed is final.
/// </summary>
public static class StatusTransitionValidator
{
    private static readonly Dictionary<RequestStatus, RequestStatus[]> AllowedTransitions = new()
    {
        [RequestStatus.New] = [RequestStatus.InProgress],
        [RequestStatus.InProgress] = [RequestStatus.Waiting, RequestStatus.Completed],
        [RequestStatus.Waiting] = [RequestStatus.InProgress],
        [RequestStatus.Completed] = []
    };

    public static bool IsAllowed(RequestStatus currentStatus, RequestStatus newStatus) =>
        AllowedTransitions.TryGetValue(currentStatus, out var allowed) && allowed.Contains(newStatus);
}
