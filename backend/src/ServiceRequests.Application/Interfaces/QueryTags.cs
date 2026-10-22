namespace ServiceRequests.Application.Interfaces;

/// <summary>Tags added with TagWith; EF writes them as a SQL comment that infrastructure can act on.</summary>
public static class QueryTags
{
    /// <summary>
    /// The best plan depends on the parameter values (e.g. how common a search prefix is), so the
    /// query must be compiled for the actual values instead of reusing a cached plan. See PERFORMANCE.md.
    /// </summary>
    public const string Recompile = "Recompile";
}
