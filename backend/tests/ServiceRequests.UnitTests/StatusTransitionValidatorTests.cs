using ServiceRequests.Domain;
using ServiceRequests.Domain.Enums;

namespace ServiceRequests.UnitTests;

public class StatusTransitionValidatorTests
{
    [Theory]
    [InlineData(RequestStatus.New, RequestStatus.InProgress)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Waiting)]
    [InlineData(RequestStatus.InProgress, RequestStatus.Completed)]
    [InlineData(RequestStatus.Waiting, RequestStatus.InProgress)]
    public void IsAllowed_ReturnsTrue_ForAllowedTransitions(RequestStatus current, RequestStatus next) =>
        Assert.True(StatusTransitionValidator.IsAllowed(current, next));

    [Theory]
    [InlineData(RequestStatus.New, RequestStatus.Waiting)]
    [InlineData(RequestStatus.New, RequestStatus.Completed)]
    [InlineData(RequestStatus.Waiting, RequestStatus.Completed)]
    [InlineData(RequestStatus.Waiting, RequestStatus.New)]
    [InlineData(RequestStatus.InProgress, RequestStatus.New)]
    [InlineData(RequestStatus.Completed, RequestStatus.InProgress)]
    [InlineData(RequestStatus.Completed, RequestStatus.New)]
    public void IsAllowed_ReturnsFalse_ForForbiddenTransitions(RequestStatus current, RequestStatus next) =>
        Assert.False(StatusTransitionValidator.IsAllowed(current, next));

    [Theory]
    [InlineData(RequestStatus.New)]
    [InlineData(RequestStatus.InProgress)]
    [InlineData(RequestStatus.Waiting)]
    [InlineData(RequestStatus.Completed)]
    public void IsAllowed_ReturnsFalse_ForSameStatus(RequestStatus status) =>
        Assert.False(StatusTransitionValidator.IsAllowed(status, status));
}
