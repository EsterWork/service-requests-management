using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ServiceRequests.Infrastructure.Persistence;

/// <summary>
/// All dates are stored as UTC. SQL Server datetime2 does not keep DateTimeKind,
/// so values read back are marked as UTC (serialized with a trailing "Z").
/// </summary>
public class UtcDateTimeConverter() : ValueConverter<DateTime, DateTime>(
    value => value,
    value => DateTime.SpecifyKind(value, DateTimeKind.Utc));
