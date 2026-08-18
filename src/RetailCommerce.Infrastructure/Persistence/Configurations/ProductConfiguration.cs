using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailCommerce.Domain.Catalog;

namespace RetailCommerce.Infrastructure.Persistence.Configurations;

public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> b)
    {
        b.ToTable("Products");

        b.Property(x => x.ItemCode).HasMaxLength(50);
        b.Property(x => x.Sku).HasMaxLength(50).IsRequired();
        b.Property(x => x.Barcode).HasMaxLength(50);
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.Unit).HasMaxLength(20).IsRequired();

        b.Property(x => x.Cost).HasPrecision(18, 2);
        b.Property(x => x.Price).HasPrecision(18, 2);
        b.Property(x => x.WholesalePrice).HasPrecision(18, 2);
        b.Property(x => x.TaxRatePercent).HasPrecision(5, 2);
        b.Property(x => x.DiscountPercent).HasPrecision(5, 2);

        b.HasIndex(x => x.Sku).IsUnique();
        b.HasIndex(x => x.Barcode).IsUnique().HasFilter("\"Barcode\" IS NOT NULL");
        b.HasIndex(x => x.ItemCode);

        b.HasOne(x => x.Department).WithMany().HasForeignKey(x => x.DepartmentId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Gender).WithMany().HasForeignKey(x => x.GenderId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.EventType).WithMany().HasForeignKey(x => x.EventTypeId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Subcategory).WithMany().HasForeignKey(x => x.SubcategoryId).OnDelete(DeleteBehavior.Restrict);
        b.HasOne(x => x.Collection).WithMany().HasForeignKey(x => x.CollectionId).OnDelete(DeleteBehavior.SetNull);
        b.HasOne(x => x.Supplier).WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.SetNull);

        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
    }
}

public class ProductAttributeValueConfiguration : IEntityTypeConfiguration<ProductAttributeValue>
{
    public void Configure(EntityTypeBuilder<ProductAttributeValue> b)
    {
        b.ToTable("ProductAttributeValues");
        b.HasIndex(x => new { x.ProductId, x.ProductAttributeTypeId }).IsUnique();

        b.HasOne(x => x.Product)
            .WithMany(p => p.AttributeValues)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);

        b.HasOne(x => x.ProductAttributeType)
            .WithMany()
            .HasForeignKey(x => x.ProductAttributeTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        b.HasOne(x => x.ProductAttributeOption)
            .WithMany()
            .HasForeignKey(x => x.ProductAttributeOptionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ProductBarcodeConfiguration : IEntityTypeConfiguration<ProductBarcode>
{
    public void Configure(EntityTypeBuilder<ProductBarcode> b)
    {
        b.ToTable("ProductBarcodes");

        b.Property(x => x.Code).HasMaxLength(60).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
        b.HasIndex(x => new { x.ProductId, x.IsPrimary });

        b.HasOne(x => x.Product)
            .WithMany(p => p.Barcodes)
            .HasForeignKey(x => x.ProductId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductFieldConfigConfiguration : IEntityTypeConfiguration<ProductFieldConfig>
{
    public void Configure(EntityTypeBuilder<ProductFieldConfig> b)
    {
        b.ToTable("ProductFieldConfigs");
        b.Property(x => x.FieldKey).HasMaxLength(50).IsRequired();
        b.HasIndex(x => x.FieldKey).IsUnique();
        b.Property(x => x.State).HasConversion<string>().HasMaxLength(20);
    }
}

public class BarcodeSettingsConfiguration : IEntityTypeConfiguration<BarcodeSettings>
{
    public void Configure(EntityTypeBuilder<BarcodeSettings> b)
    {
        b.ToTable("BarcodeSettings");
        b.Property(x => x.CompanyName).HasMaxLength(200).IsRequired();
        b.Property(x => x.LabelWidthInches).HasPrecision(5, 2);
        b.Property(x => x.LabelHeightInches).HasPrecision(5, 2);
    }
}

public class CurrencySettingsConfiguration : IEntityTypeConfiguration<CurrencySettings>
{
    public void Configure(EntityTypeBuilder<CurrencySettings> b)
    {
        b.ToTable("CurrencySettings");
        b.Property(x => x.Symbol).HasMaxLength(10).IsRequired();
    }
}

public class PosSettingsConfiguration : IEntityTypeConfiguration<PosSettings>
{
    public void Configure(EntityTypeBuilder<PosSettings> b)
    {
        b.ToTable("PosSettings");
        b.Property(x => x.ViewMode).HasMaxLength(20).IsRequired();
        // Explicit SQL-level default (not just the C# property initializer) so any row already in
        // the table before this column existed — every existing deployment's singleton row — gets
        // a sane value from the migration itself instead of silently defaulting to 0.
        b.Property(x => x.ReturnPolicyDays).HasDefaultValue(15);
        b.Property(x => x.VisibleProductFields).HasMaxLength(2000).HasDefaultValue("sku,barcode,price,totalStock");
    }
}
