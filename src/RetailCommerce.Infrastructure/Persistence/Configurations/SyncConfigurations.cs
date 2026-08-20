using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailCommerce.Domain.Sync;

namespace RetailCommerce.Infrastructure.Persistence.Configurations;

public class SyncLogConfiguration : IEntityTypeConfiguration<SyncLog>
{
    public void Configure(EntityTypeBuilder<SyncLog> b)
    {
        b.ToTable("SyncLogs");
        b.Property(x => x.EntityType).HasMaxLength(50).IsRequired();
        b.Property(x => x.ErrorMessage).HasMaxLength(1000);

        b.Property(x => x.Direction).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        b.HasIndex(x => x.TerminalId);
        b.HasIndex(x => x.OccurredAtUtc);
    }
}
