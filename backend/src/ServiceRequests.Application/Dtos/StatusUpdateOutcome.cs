namespace ServiceRequests.Application.Dtos;

public enum StatusUpdateOutcome
{
    Succeeded,
    NotFound,
    InvalidTransition,
    Conflict
}
