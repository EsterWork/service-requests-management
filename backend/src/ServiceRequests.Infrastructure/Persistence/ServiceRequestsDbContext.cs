using Microsoft.EntityFrameworkCore;
using ServiceRequests.Application.Interfaces;
using ServiceRequests.Domain.Entities;

namespace ServiceRequests.Infrastructure.Persistence;

public class ServiceRequestsDbContext(DbContextOptions<ServiceRequestsDbContext> options) : DbContext(options), IApplicationDbContext
{
    public DbSet<ServiceRequest> ServiceRequests => Set<ServiceRequest>();
    public DbSet<RequestStatusHistory> RequestStatusHistory => Set<RequestStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ServiceRequestsDbContext).Assembly);

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder) =>
        configurationBuilder.Properties<DateTime>().HaveConversion<UtcDateTimeConverter>();
}
