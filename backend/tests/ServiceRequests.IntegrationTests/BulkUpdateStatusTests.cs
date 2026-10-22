using System.Net;
using System.Net.Http.Json;
using ServiceRequests.Application.Dtos;
using ServiceRequests.Domain.Enums;
using ServiceRequests.IntegrationTests.Infrastructure;

namespace ServiceRequests.IntegrationTests;

public class BulkUpdateStatusTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task BulkUpdateStatus_MixedItems_ProcessesEachItemIndependently()
    {
        var requests = await Factory.AddRequestsAsync(
            TestData.Request(RequestStatus.InProgress),
            TestData.Request(RequestStatus.InProgress),
            TestData.Request(RequestStatus.InProgress),
            TestData.Request(RequestStatus.Completed));
        var (first, second, conflicting, completed) = (requests[0], requests[1], requests[2], requests[3]);
        const int missingId = int.MaxValue;

        var response = await Client.PatchAsJsonAsync($"{RequestsUrl}/status/bulk", new BulkUpdateStatusRequest
        {
            NewStatus = RequestStatus.Waiting,
            ChangedBy = "alice",
            Items =
            [
                new() { Id = first.Id, RowVersion = TestData.RowVersionOf(first) },
                new() { Id = missingId, RowVersion = TestData.StaleRowVersion },
                new() { Id = conflicting.Id, RowVersion = TestData.StaleRowVersion },
                new() { Id = second.Id, RowVersion = TestData.RowVersionOf(second) },
                new() { Id = completed.Id, RowVersion = TestData.RowVersionOf(completed) }
            ]
        }, Json);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var result = (await response.Content.ReadFromJsonAsync<BulkUpdateStatusResult>(Json))!;

        Assert.Equal(
            [
                (first.Id, StatusUpdateOutcome.Succeeded),
                (missingId, StatusUpdateOutcome.NotFound),
                (conflicting.Id, StatusUpdateOutcome.Conflict),
                (second.Id, StatusUpdateOutcome.Succeeded),
                (completed.Id, StatusUpdateOutcome.InvalidTransition)
            ],
            result.Results.Select(item => (item.Id, item.Outcome)));
        Assert.Equal(new BulkUpdateSummary(Total: 5, Succeeded: 2, NotFound: 1, InvalidTransition: 1, Conflict: 1), result.Summary);

        // A failed item must not block or roll back the successful ones.
        Assert.Equal(RequestStatus.Waiting, (await Factory.GetRequestAsync(first.Id)).Status);
        Assert.Equal(RequestStatus.Waiting, (await Factory.GetRequestAsync(second.Id)).Status);
        Assert.Equal(RequestStatus.InProgress, (await Factory.GetRequestAsync(conflicting.Id)).Status);
        Assert.Single(await Factory.GetHistoryAsync(first.Id));
        Assert.Single(await Factory.GetHistoryAsync(second.Id));
        Assert.Empty(await Factory.GetHistoryAsync(conflicting.Id));
        Assert.Empty(await Factory.GetHistoryAsync(completed.Id));
    }
}
