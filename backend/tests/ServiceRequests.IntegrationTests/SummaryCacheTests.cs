using System.Net.Http.Json;
using ServiceRequests.Application.Dtos;
using ServiceRequests.Domain.Enums;
using ServiceRequests.IntegrationTests.Infrastructure;

namespace ServiceRequests.IntegrationTests;

public class SummaryCacheTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task Summary_AfterStatusUpdate_ReturnsFreshCountsInsteadOfCachedOnes()
    {
        var requests = await Factory.AddRequestsAsync(
            TestData.Request(RequestStatus.InProgress),
            TestData.Request(RequestStatus.InProgress));

        var before = await GetSummaryAsync();
        await Client.PatchAsJsonAsync($"{RequestsUrl}/{requests[0].Id}/status", new UpdateStatusRequest
        {
            NewStatus = RequestStatus.Waiting,
            RowVersion = TestData.RowVersionOf(requests[0]),
            ChangedBy = "alice"
        }, Json);
        var after = await GetSummaryAsync();

        Assert.Equal((2, 0), (CountOf(before, RequestStatus.InProgress), CountOf(before, RequestStatus.Waiting)));
        Assert.Equal((1, 1), (CountOf(after, RequestStatus.InProgress), CountOf(after, RequestStatus.Waiting)));
    }

    private async Task<RequestSummaryDto> GetSummaryAsync() =>
        (await Client.GetFromJsonAsync<RequestSummaryDto>($"{RequestsUrl}/summary", Json))!;

    private static int CountOf(RequestSummaryDto summary, RequestStatus status) =>
        summary.ByStatus.Single(item => item.Status == status).Count;
}
