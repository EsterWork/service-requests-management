using ServiceRequests.Domain.Entities;
using ServiceRequests.Domain.Enums;

namespace ServiceRequests.IntegrationTests.Infrastructure;

public static class TestData
{
    public const string DefaultOrganization = "Test Organization Ltd";

    private static readonly DateTime BaseDate = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    // A well-formed rowVersion that never matches a real row.
    public static readonly string StaleRowVersion = Convert.ToBase64String(BitConverter.GetBytes(1L));

    public static ServiceRequest Request(
        RequestStatus status = RequestStatus.New,
        RequestPriority priority = RequestPriority.Medium,
        string title = "Test request",
        string organizationName = DefaultOrganization,
        int createdDay = 0)
    {
        var createdAt = BaseDate.AddDays(createdDay);
        return new ServiceRequest
        {
            Title = title,
            OrganizationName = organizationName,
            Status = status,
            Priority = priority,
            AssignedTo = "Dana Cohen",
            CreatedAt = createdAt,
            UpdatedAt = createdAt
        };
    }

    public static string RowVersionOf(ServiceRequest request) => Convert.ToBase64String(request.RowVersion);
}
