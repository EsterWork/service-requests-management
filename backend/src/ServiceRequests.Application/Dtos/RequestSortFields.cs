namespace ServiceRequests.Application.Dtos;

public static class RequestSortFields
{
    public const string CreatedAt = "createdAt";
    public const string UpdatedAt = "updatedAt";
    public const string Priority = "priority";
    public const string Title = "title";

    public static readonly IReadOnlyList<string> All = [CreatedAt, UpdatedAt, Priority, Title];
}
