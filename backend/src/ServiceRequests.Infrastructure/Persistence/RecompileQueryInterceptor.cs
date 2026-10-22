using System.Data.Common;
using Microsoft.EntityFrameworkCore.Diagnostics;
using ServiceRequests.Application.Interfaces;

namespace ServiceRequests.Infrastructure.Persistence;

/// <summary>
/// Appends OPTION (RECOMPILE) to queries tagged with <see cref="QueryTags.Recompile"/>.
/// Without it, SQL Server reuses the plan compiled for the first search value; a plan that is ideal
/// for a common prefix scans the whole table for a rare one (measured in PERFORMANCE.md).
/// </summary>
public class RecompileQueryInterceptor : DbCommandInterceptor
{
    private const string TagComment = $"-- {QueryTags.Recompile}";

    public override InterceptionResult<DbDataReader> ReaderExecuting(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result)
    {
        AddRecompileHint(command);
        return result;
    }

    public override ValueTask<InterceptionResult<DbDataReader>> ReaderExecutingAsync(
        DbCommand command, CommandEventData eventData, InterceptionResult<DbDataReader> result,
        CancellationToken cancellationToken = default)
    {
        AddRecompileHint(command);
        return ValueTask.FromResult(result);
    }

    public static void AddRecompileHint(DbCommand command)
    {
        if (command.CommandText.StartsWith(TagComment, StringComparison.Ordinal))
        {
            command.CommandText += " OPTION (RECOMPILE)";
        }
    }
}
