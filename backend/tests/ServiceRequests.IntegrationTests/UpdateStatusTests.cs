using System.Net;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Mvc;
using ServiceRequests.Application.Dtos;
using ServiceRequests.Domain.Entities;
using ServiceRequests.Domain.Enums;
using ServiceRequests.IntegrationTests.Infrastructure;

namespace ServiceRequests.IntegrationTests;

public class UpdateStatusTests(ApiFactory factory) : IntegrationTestBase(factory)
{
    [Fact]
    public async Task UpdateStatus_ValidTransition_UpdatesRequestAndRecordsHistory()
    {
        var (request, _) = await AddSingleAsync(RequestStatus.New);

        var response = await PatchStatusAsync(request.Id, RequestStatus.InProgress, TestData.RowVersionOf(request), "alice");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var updated = (await response.Content.ReadFromJsonAsync<ServiceRequestDto>(Json))!;
        Assert.Equal(RequestStatus.InProgress, updated.Status);
        Assert.NotEqual(TestData.RowVersionOf(request), updated.RowVersion);
        Assert.True(updated.UpdatedAt > request.UpdatedAt);

        var history = await Client.GetFromJsonAsync<List<RequestStatusHistoryDto>>($"{RequestsUrl}/{request.Id}/history", Json);
        var entry = Assert.Single(history!);
        Assert.Equal((RequestStatus.New, RequestStatus.InProgress, "alice"), (entry.PreviousStatus, entry.NewStatus, entry.ChangedBy));
    }

    [Fact]
    public async Task UpdateStatus_TwoConcurrentUpdatesWithSameRowVersion_OneSucceedsOneConflictsAndOneAuditRow()
    {
        var (request, rowVersion) = await AddSingleAsync(RequestStatus.InProgress);

        // Both requests wait on the same gate and are released together, so they really run in parallel on the server.
        var gate = new TaskCompletionSource();
        var updates = new[] { "alice", "bob" }.Select(user => Task.Run(async () =>
        {
            await gate.Task;
            return await PatchStatusAsync(request.Id, RequestStatus.Waiting, rowVersion, user);
        })).ToList();
        gate.SetResult();
        var responses = await Task.WhenAll(updates);

        Assert.Equal([HttpStatusCode.OK, HttpStatusCode.Conflict], responses.Select(response => response.StatusCode).Order());
        Assert.Single(await Factory.GetHistoryAsync(request.Id));
        Assert.Equal(RequestStatus.Waiting, (await Factory.GetRequestAsync(request.Id)).Status);
    }

    [Fact]
    public async Task UpdateStatus_ForbiddenTransition_Returns400AndCreatesNoAuditRow()
    {
        var (request, rowVersion) = await AddSingleAsync(RequestStatus.Completed);

        var response = await PatchStatusAsync(request.Id, RequestStatus.InProgress, rowVersion, "alice");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var problem = await response.Content.ReadFromJsonAsync<ValidationProblemDetails>(Json);
        Assert.Contains("newStatus", problem!.Errors.Keys);
        Assert.Empty(await Factory.GetHistoryAsync(request.Id));
        Assert.Equal(RequestStatus.Completed, (await Factory.GetRequestAsync(request.Id)).Status);
    }

    [Fact]
    public async Task UpdateStatus_UnknownRequest_Returns404()
    {
        var response = await PatchStatusAsync(int.MaxValue, RequestStatus.InProgress, TestData.StaleRowVersion, "alice");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<(ServiceRequest Request, string RowVersion)> AddSingleAsync(RequestStatus status)
    {
        var request = (await Factory.AddRequestsAsync(TestData.Request(status)))[0];
        return (request, TestData.RowVersionOf(request));
    }

    private Task<HttpResponseMessage> PatchStatusAsync(int id, RequestStatus newStatus, string rowVersion, string changedBy) =>
        Client.PatchAsJsonAsync($"{RequestsUrl}/{id}/status",
            new UpdateStatusRequest { NewStatus = newStatus, RowVersion = rowVersion, ChangedBy = changedBy }, Json);
}
