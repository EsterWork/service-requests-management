using ServiceRequests.Domain.Enums;

namespace ServiceRequests.Domain.Entities;

public class ServiceRequest
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string OrganizationName { get; set; } = string.Empty;
    public RequestStatus Status { get; set; }
    public RequestPriority Priority { get; set; }
    public string? AssignedTo { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<RequestStatusHistory> StatusHistory { get; } = new List<RequestStatusHistory>();

    /// <summary>
    /// Changes the status and records the audit entry in the same unit of work,
    /// so both are persisted by a single SaveChanges (one transaction).
    /// </summary>
    public void ChangeStatus(RequestStatus newStatus, string changedBy, DateTime utcNow)
    {
        if (!StatusTransitionValidator.IsAllowed(Status, newStatus))
        {
            throw new InvalidOperationException($"Transition from {Status} to {newStatus} is not allowed.");
        }

        StatusHistory.Add(new RequestStatusHistory
        {
            RequestId = Id,
            PreviousStatus = Status,
            NewStatus = newStatus,
            ChangedAt = utcNow,
            ChangedBy = changedBy
        });

        Status = newStatus;
        UpdatedAt = utcNow;
    }
}
