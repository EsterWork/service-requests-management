using ServiceRequests.Domain.Enums;

namespace ServiceRequests.Domain.Entities;

public class RequestStatusHistory
{
    public long Id { get; set; }
    public int RequestId { get; set; }
    public RequestStatus PreviousStatus { get; set; }
    public RequestStatus NewStatus { get; set; }
    public DateTime ChangedAt { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
}
