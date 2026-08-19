using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailCommerce.Domain.Sales;

namespace RetailCommerce.Infrastructure.Persistence.Configurations;

public class PosTerminalConfiguration : IEntityTypeConfiguration<PosTerminal>
{
    public void Configure(EntityTypeBuilder<PosTerminal> b)
    {
        b.ToTable("PosTerminals");
        b.Property(x => x.Code).HasMaxLength(30).IsRequired();
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();

        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PosTerminalUserConfiguration : IEntityTypeConfiguration<PosTerminalUser>
{
    public void Configure(EntityTypeBuilder<PosTerminalUser> b)
    {
        b.ToTable("PosTerminalUsers");
        b.HasKey(x => new { x.TerminalId, x.UserId });

        b.HasOne(x => x.Terminal).WithMany(t => t.AssignedUsers).HasForeignKey(x => x.TerminalId).OnDelete(DeleteBehavior.Cascade);
    }
}
