using ServiceRequests.Domain.Enums;

namespace ServiceRequests.Application.Dtos;

public class BulkUpdateStatusRequest
{
    public const int MaxItems = 100;

    public RequestStatus? NewStatus { get; set; }
    public string ChangedBy { get; set; } = string.Empty;
    public List<BulkUpdateStatusItem> Items { get; set; } = [];
}

public class BulkUpdateStatusItem
{
    public int Id { get; set; }
    public string RowVersion { get; set; } = string.Empty;
}
