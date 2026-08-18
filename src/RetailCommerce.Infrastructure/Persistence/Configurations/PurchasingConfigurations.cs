using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailCommerce.Domain.Purchasing;

namespace RetailCommerce.Infrastructure.Persistence.Configurations;

public class PurchaseOrderConfiguration : IEntityTypeConfiguration<PurchaseOrder>
{
    public void Configure(EntityTypeBuilder<PurchaseOrder> b)
    {
        b.ToTable("PurchaseOrders");
        // See OrderConfiguration.OrderNumber for why this is 100, not the length of any
        // realistic generated value.
        b.Property(x => x.PoNumber).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.PoNumber).IsUnique();
        b.Property(x => x.Reference).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        b.Property(x => x.Subtotal).HasPrecision(18, 2);
        b.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        b.Property(x => x.TaxAmount).HasPrecision(18, 2);
        b.Property(x => x.Total).HasPrecision(18, 2);

        b.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseOrderLineConfiguration : IEntityTypeConfiguration<PurchaseOrderLine>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderLine> b)
    {
        b.ToTable("PurchaseOrderLines");
        b.Property(x => x.Sku).HasMaxLength(50).IsRequired();
        b.Property(x => x.UnitCost).HasPrecision(18, 2);
        b.Property(x => x.DiscountPercent).HasPrecision(5, 2);
        b.Property(x => x.TaxPercent).HasPrecision(5, 2);
        b.Property(x => x.LineTotal).HasPrecision(18, 2);

        b.HasOne(x => x.PurchaseOrder).WithMany(o => o.Lines).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PurchaseOrderSettingsConfiguration : IEntityTypeConfiguration<PurchaseOrderSettings>
{
    public void Configure(EntityTypeBuilder<PurchaseOrderSettings> b)
    {
        b.ToTable("PurchaseOrderSettings");
    }
}

public class TransferConfiguration : IEntityTypeConfiguration<Transfer>
{
    public void Configure(EntityTypeBuilder<Transfer> b)
    {
        b.ToTable("Transfers");
        b.Property(x => x.TransferNumber).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.TransferNumber).IsUnique();
        b.Property(x => x.Reference).HasMaxLength(100);
        b.Property(x => x.Notes).HasMaxLength(500);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);

        b.HasOne(x => x.FromWarehouse).WithMany().HasForeignKey(x => x.FromWarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.ToWarehouse).WithMany().HasForeignKey(x => x.ToWarehouseId).OnDelete(DeleteBehavior.Restrict);

        b.ToTable(t => t.HasCheckConstraint("CK_Transfers_DifferentWarehouses", "\"FromWarehouseId\" <> \"ToWarehouseId\""));
    }
}

public class TransferLineConfiguration : IEntityTypeConfiguration<TransferLine>
{
    public void Configure(EntityTypeBuilder<TransferLine> b)
    {
        b.ToTable("TransferLines");
        b.Property(x => x.Unit).HasMaxLength(20).IsRequired();

        b.HasOne(x => x.Transfer).WithMany(t => t.Lines).HasForeignKey(x => x.TransferId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
