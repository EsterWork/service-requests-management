using Microsoft.EntityFrameworkCore;
using ServiceRequests.Domain.Entities;

namespace ServiceRequests.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ServiceRequest> ServiceRequests { get; }
    DbSet<RequestStatusHistory> RequestStatusHistory { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
