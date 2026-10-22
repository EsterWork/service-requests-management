using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceRequests.Domain.Entities;

namespace ServiceRequests.Infrastructure.Persistence.Configurations;

public class ServiceRequestConfiguration : IEntityTypeConfiguration<ServiceRequest>
{
    public void Configure(EntityTypeBuilder<ServiceRequest> builder)
    {
        builder.ToTable("ServiceRequests");

        builder.HasKey(request => request.Id);

        builder.Property(request => request.Title).HasMaxLength(200).IsRequired();
        builder.Property(request => request.OrganizationName).HasMaxLength(200).IsRequired();
        builder.Property(request => request.AssignedTo).HasMaxLength(100);
        builder.Property(request => request.RowVersion).IsRowVersion();

        builder.HasMany(request => request.StatusHistory)
            .WithOne()
            .HasForeignKey(history => history.RequestId)
            .OnDelete(DeleteBehavior.Cascade);

        // Filter + default sort (newest first) for the most common list queries.
        builder.HasIndex(request => new { request.Status, request.CreatedAt });
        builder.HasIndex(request => new { request.Priority, request.CreatedAt });
        builder.HasIndex(request => request.AssignedTo);
        builder.HasIndex(request => request.OrganizationName);
        builder.HasIndex(request => request.CreatedAt);

        // Supports the prefix search (LIKE 'term%') on Title; OrganizationName is already indexed above.
        builder.HasIndex(request => request.Title);
    }
}
