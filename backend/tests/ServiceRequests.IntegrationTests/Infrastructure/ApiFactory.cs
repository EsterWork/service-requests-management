using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServiceRequests.Application.Services;
using ServiceRequests.Domain.Entities;
using ServiceRequests.Infrastructure.Persistence;

namespace ServiceRequests.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API in memory against a dedicated SQL Server test database.
/// </summary>
public class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private const string TestDatabaseSuffix = "_Tests";

    private const string DefaultConnectionString =
        "Server=.\\SQLEXPRESS;Database=ServiceRequests_Tests;Trusted_Connection=True;TrustServerCertificate=True";

    // Allows CI or Docker to point the tests at another SQL Server.
    private static readonly string ConnectionString =
        Environment.GetEnvironmentVariable("SERVICE_REQUESTS_TEST_DB") ?? DefaultConnectionString;

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.UseSetting("ConnectionStrings:ServiceRequestsDb", ConnectionString);

    public async Task InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServiceRequestsDbContext>();

        // Tests delete all data, so refuse to run against anything but a dedicated test database.
        var databaseName = dbContext.Database.GetDbConnection().Database;
        if (!databaseName.EndsWith(TestDatabaseSuffix, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException($"Integration tests must use a '*{TestDatabaseSuffix}' database, not '{databaseName}'.");
        }

        await dbContext.Database.MigrateAsync();
    }

    public async Task ResetAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServiceRequestsDbContext>();

        await dbContext.RequestStatusHistory.ExecuteDeleteAsync();
        await dbContext.ServiceRequests.ExecuteDeleteAsync();
        scope.ServiceProvider.GetRequiredService<IRequestSummaryService>().InvalidateCache();
    }

    /// <summary>Inserts the requests and returns them with their database Id and RowVersion.</summary>
    public async Task<ServiceRequest[]> AddRequestsAsync(params ServiceRequest[] requests)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServiceRequestsDbContext>();

        dbContext.ServiceRequests.AddRange(requests);
        await dbContext.SaveChangesAsync();
        return requests;
    }

    public async Task<ServiceRequest> GetRequestAsync(int id)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServiceRequestsDbContext>();

        return await dbContext.ServiceRequests.AsNoTracking().SingleAsync(request => request.Id == id);
    }

    public async Task<List<RequestStatusHistory>> GetHistoryAsync(int requestId)
    {
        await using var scope = Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<ServiceRequestsDbContext>();

        return await dbContext.RequestStatusHistory.AsNoTracking().Where(history => history.RequestId == requestId).ToListAsync();
    }

    Task IAsyncLifetime.DisposeAsync() => DisposeAsync().AsTask();
}
