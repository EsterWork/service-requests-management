using System.Data;
using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ServiceRequests.Domain.Entities;
using ServiceRequests.Infrastructure.Persistence;

namespace ServiceRequests.Infrastructure.Seeding;

/// <summary>
/// Re-creates the test data set: applies migrations, deletes existing data and bulk-inserts fresh requests.
/// </summary>
public class DatabaseSeeder(ServiceRequestsDbContext dbContext, ILogger<DatabaseSeeder> logger)
{
    private const int RequestCount = 100_000;
    private const int BatchSize = 10_000;

    public async Task SeedAsync(CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("Seeding started: {RequestCount} requests", RequestCount);

        await dbContext.Database.MigrateAsync(cancellationToken);
        await ClearDataAsync(cancellationToken);
        await InsertRequestsAsync(cancellationToken);

        logger.LogInformation("Seeding completed: {RequestCount} requests in {ElapsedSeconds:F1}s",
            RequestCount, stopwatch.Elapsed.TotalSeconds);
    }

    private async Task ClearDataAsync(CancellationToken cancellationToken)
    {
        await dbContext.RequestStatusHistory.ExecuteDeleteAsync(cancellationToken);
        await dbContext.ServiceRequests.ExecuteDeleteAsync(cancellationToken);
    }

    private async Task InsertRequestsAsync(CancellationToken cancellationToken)
    {
        // KeepIdentity: ids are generated as 1..N so every seed run produces the same data set.
        using var bulkCopy = new SqlBulkCopy(dbContext.Database.GetConnectionString(), SqlBulkCopyOptions.KeepIdentity | SqlBulkCopyOptions.TableLock)
        {
            DestinationTableName = dbContext.Model.FindEntityType(typeof(ServiceRequest))!.GetTableName(),
            BatchSize = BatchSize,
            BulkCopyTimeout = 0
        };

        using var table = CreateRequestsTable();
        foreach (DataColumn column in table.Columns)
        {
            bulkCopy.ColumnMappings.Add(column.ColumnName, column.ColumnName);
        }

        var generator = new ServiceRequestDataGenerator(DateTime.UtcNow);
        foreach (var batch in generator.Generate(RequestCount).Chunk(BatchSize))
        {
            table.Clear();
            foreach (var request in batch)
            {
                table.Rows.Add(request.Id, request.Title, request.OrganizationName, (int)request.Status,
                    (int)request.Priority, (object?)request.AssignedTo ?? DBNull.Value, request.CreatedAt, request.UpdatedAt);
            }

            await bulkCopy.WriteToServerAsync(table, cancellationToken);
        }
    }

    private static DataTable CreateRequestsTable()
    {
        var table = new DataTable();
        table.Columns.Add(nameof(ServiceRequest.Id), typeof(int));
        table.Columns.Add(nameof(ServiceRequest.Title), typeof(string));
        table.Columns.Add(nameof(ServiceRequest.OrganizationName), typeof(string));
        table.Columns.Add(nameof(ServiceRequest.Status), typeof(int));
        table.Columns.Add(nameof(ServiceRequest.Priority), typeof(int));
        table.Columns.Add(nameof(ServiceRequest.AssignedTo), typeof(string));
        table.Columns.Add(nameof(ServiceRequest.CreatedAt), typeof(DateTime));
        table.Columns.Add(nameof(ServiceRequest.UpdatedAt), typeof(DateTime));
        return table;
    }
}
