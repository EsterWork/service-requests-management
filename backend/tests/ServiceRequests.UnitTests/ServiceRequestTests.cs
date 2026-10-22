using ServiceRequests.Domain.Entities;
using ServiceRequests.Domain.Enums;

namespace ServiceRequests.UnitTests;

public class ServiceRequestTests
{
    private static readonly DateTime Now = new(2026, 1, 15, 10, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void ChangeStatus_AllowedTransition_UpdatesStatusAndRecordsHistory()
    {
        var request = new ServiceRequest { Id = 7, Status = RequestStatus.New, UpdatedAt = Now.AddDays(-1) };

        request.ChangeStatus(RequestStatus.InProgress, "dana", Now);

        Assert.Equal(RequestStatus.InProgress, request.Status);
        Assert.Equal(Now, request.UpdatedAt);
        var history = Assert.Single(request.StatusHistory);
        Assert.Equal(7, history.RequestId);
        Assert.Equal(RequestStatus.New, history.PreviousStatus);
        Assert.Equal(RequestStatus.InProgress, history.NewStatus);
        Assert.Equal(Now, history.ChangedAt);
        Assert.Equal("dana", history.ChangedBy);
    }

    [Fact]
    public void ChangeStatus_ForbiddenTransition_ThrowsAndLeavesRequestUnchanged()
    {
        var updatedAt = Now.AddDays(-1);
        var request = new ServiceRequest { Status = RequestStatus.Completed, UpdatedAt = updatedAt };

        Assert.Throws<InvalidOperationException>(() => request.ChangeStatus(RequestStatus.InProgress, "dana", Now));

        Assert.Equal(RequestStatus.Completed, request.Status);
        Assert.Equal(updatedAt, request.UpdatedAt);
        Assert.Empty(request.StatusHistory);
    }
}
