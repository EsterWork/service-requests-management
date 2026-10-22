using ServiceRequests.Domain.Enums;

namespace ServiceRequests.Application.Dtos;

public record RequestStatusHistoryDto(
    long Id,
    int RequestId,
    RequestStatus PreviousStatus,
    RequestStatus NewStatus,
    DateTime ChangedAt,
    string ChangedBy);
