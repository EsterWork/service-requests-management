namespace ServiceRequests.Domain.Enums;

// Values are ordered by severity so that sorting by the stored int sorts by priority.
public enum RequestPriority
{
    Low = 1,
    Medium = 2,
    High = 3
}
