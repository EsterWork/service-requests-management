namespace ServiceRequests.Application.Dtos;

public static class SortDirections
{
    public const string Ascending = "asc";
    public const string Descending = "desc";

    public static readonly IReadOnlyList<string> All = [Ascending, Descending];
}
