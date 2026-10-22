using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServiceRequests.Domain.Entities;

namespace ServiceRequests.Infrastructure.Persistence.Configurations;

public class RequestStatusHistoryConfiguration : IEntityTypeConfiguration<RequestStatusHistory>
{
    public void Configure(EntityTypeBuilder<RequestStatusHistory> builder)
    {
        builder.ToTable("RequestStatusHistory");

        builder.HasKey(history => history.Id);

        builder.Property(history => history.ChangedBy).HasMaxLength(100).IsRequired();

        builder.HasIndex(history => new { history.RequestId, history.ChangedAt });
    }
}
