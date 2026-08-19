using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailCommerce.Domain.Sales;

namespace RetailCommerce.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> b)
    {
        b.ToTable("Orders");
        // 100 as a generous ceiling — DocumentNumberService generates a short "{2-letter
        // prefix}{sequence}" value (well under 10 characters) with no store/warehouse code
        // embedded, so this should never come close to being hit; it's just a safety margin
        // against a Postgres "value too long" error, not the actual expected length.
        b.Property(x => x.OrderNumber).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.OrderNumber).IsUnique();

        b.Property(x => x.Subtotal).HasPrecision(18, 2);
        b.Property(x => x.DiscountAmount).HasPrecision(18, 2);
        b.Property(x => x.TaxAmount).HasPrecision(18, 2);
        b.Property(x => x.Total).HasPrecision(18, 2);
        b.Property(x => x.Notes).HasMaxLength(500);

        b.Property(x => x.Channel).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.PaymentMethod).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.DiscountLabel).HasMaxLength(150);

        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.SalesPerson).WithMany().HasForeignKey(x => x.SalesPersonId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Terminal).WithMany().HasForeignKey(x => x.TerminalId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class OrderLineConfiguration : IEntityTypeConfiguration<OrderLine>
{
    public void Configure(EntityTypeBuilder<OrderLine> b)
    {
        b.ToTable("OrderLines");
        b.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.TaxRatePercent).HasPrecision(5, 2);
        b.Property(x => x.DiscountPercent).HasPrecision(5, 2);
        b.Property(x => x.LineTotal).HasPrecision(18, 2);

        b.HasOne(x => x.Order).WithMany(o => o.Lines).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}

public class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> b)
    {
        b.ToTable("Payments");
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Method).HasConversion<string>().HasMaxLength(20);

        b.HasOne(x => x.Order).WithMany(o => o.Payments).HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class DiscountConfiguration : IEntityTypeConfiguration<Discount>
{
    public void Configure(EntityTypeBuilder<Discount> b)
    {
        b.ToTable("Discounts");
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Value).HasPrecision(18, 2);

        b.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Cascade);
    }
}

public class ReturnConfiguration : IEntityTypeConfiguration<Return>
{
    public void Configure(EntityTypeBuilder<Return> b)
    {
        b.ToTable("Returns");
        b.Property(x => x.ReturnNumber).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.ReturnNumber).IsUnique();
        b.Property(x => x.Reason).HasMaxLength(300);
        b.Property(x => x.Total).HasPrecision(18, 2);

        b.HasOne(x => x.Order).WithMany().HasForeignKey(x => x.OrderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Customer).WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
    }
}

public class ReturnLineConfiguration : IEntityTypeConfiguration<ReturnLine>
{
    public void Configure(EntityTypeBuilder<ReturnLine> b)
    {
        b.ToTable("ReturnLines");
        b.Property(x => x.ProductName).HasMaxLength(200).IsRequired();
        b.Property(x => x.UnitPrice).HasPrecision(18, 2);
        b.Property(x => x.LineTotal).HasPrecision(18, 2);

        b.HasOne(x => x.Return).WithMany(r => r.Lines).HasForeignKey(x => x.ReturnId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.OrderLine).WithMany().HasForeignKey(x => x.OrderLineId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Product).WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
    }
}
