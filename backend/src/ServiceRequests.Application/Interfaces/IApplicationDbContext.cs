using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using ServiceRequests.Domain.Entities;

namespace ServiceRequests.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<ServiceRequest> ServiceRequests { get; }
    DbSet<RequestStatusHistory> RequestStatusHistory { get; }

    EntityEntry<TEntity> Entry<TEntity>(TEntity entity) where TEntity : class;

    Task<int> SaveChangesAsync(CancellationToken cancellationToken);
}
