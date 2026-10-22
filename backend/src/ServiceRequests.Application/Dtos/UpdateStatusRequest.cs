using ServiceRequests.Domain.Enums;

namespace ServiceRequests.Application.Dtos;

public class UpdateStatusRequest
{
    public RequestStatus? NewStatus { get; set; }

    /// <summary>Base64 RowVersion the client received with the request; used for optimistic concurrency.</summary>
    public string RowVersion { get; set; } = string.Empty;

    public string ChangedBy { get; set; } = string.Empty;
}
