using ServiceRequests.Domain.Enums;

namespace ServiceRequests.Application.Dtos;

public record RequestSummaryDto(IReadOnlyList<StatusCount> ByStatus, IReadOnlyList<PriorityCount> ByPriority);

public record StatusCount(RequestStatus Status, int Count);

public record PriorityCount(RequestPriority Priority, int Count);
