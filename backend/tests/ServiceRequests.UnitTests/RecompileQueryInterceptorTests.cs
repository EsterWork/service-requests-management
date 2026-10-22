using Microsoft.Data.SqlClient;
using ServiceRequests.Application.Interfaces;
using ServiceRequests.Infrastructure.Persistence;

namespace ServiceRequests.UnitTests;

public class RecompileQueryInterceptorTests
{
    [Fact]
    public void AddRecompileHint_TaggedQuery_AppendsOptionRecompile()
    {
        using var command = new SqlCommand($"-- {QueryTags.Recompile}\r\n\r\nSELECT COUNT(*) FROM [ServiceRequests]");

        RecompileQueryInterceptor.AddRecompileHint(command);

        Assert.EndsWith("FROM [ServiceRequests] OPTION (RECOMPILE)", command.CommandText);
    }

    [Fact]
    public void AddRecompileHint_UntaggedQuery_LeavesCommandUnchanged()
    {
        const string sql = "SELECT COUNT(*) FROM [ServiceRequests]";
        using var command = new SqlCommand(sql);

        RecompileQueryInterceptor.AddRecompileHint(command);

        Assert.Equal(sql, command.CommandText);
    }
}
