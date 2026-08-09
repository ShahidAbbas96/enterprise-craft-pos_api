using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailCommerce.Domain.Taxonomy;

namespace RetailCommerce.Infrastructure.Persistence.Configurations;

public class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> b)
    {
        b.ToTable("Departments");
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public class GenderConfiguration : IEntityTypeConfiguration<Gender>
{
    public void Configure(EntityTypeBuilder<Gender> b)
    {
        b.ToTable("Genders");
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public class EventTypeConfiguration : IEntityTypeConfiguration<EventType>
{
    public void Configure(EntityTypeBuilder<EventType> b)
    {
        b.ToTable("EventTypes");
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public class CategoryConfiguration : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> b)
    {
        b.ToTable("Categories");
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(x => new { x.DepartmentId, x.Code }).IsUnique();
        b.HasOne(x => x.Department)
            .WithMany(d => d.Categories)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class SubcategoryConfiguration : IEntityTypeConfiguration<Subcategory>
{
    public void Configure(EntityTypeBuilder<Subcategory> b)
    {
        b.ToTable("Subcategories");
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(x => new { x.CategoryId, x.Code }).IsUnique();
        b.HasOne(x => x.Category)
            .WithMany(c => c.Subcategories)
            .HasForeignKey(x => x.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class CollectionConfiguration : IEntityTypeConfiguration<Collection>
{
    public void Configure(EntityTypeBuilder<Collection> b)
    {
        b.ToTable("Collections");
        b.Property(x => x.Name).HasMaxLength(150).IsRequired();
        b.Property(x => x.VersionLabel).HasMaxLength(20).IsRequired();
        b.Ignore(x => x.DisplayCode);
        b.HasOne(x => x.Department)
            .WithMany()
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}

public class ProductAttributeTypeConfiguration : IEntityTypeConfiguration<ProductAttributeType>
{
    public void Configure(EntityTypeBuilder<ProductAttributeType> b)
    {
        b.ToTable("ProductAttributeTypes");
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(x => new { x.DepartmentId, x.Code }).IsUnique();
        b.HasOne(x => x.Department)
            .WithMany(d => d.AttributeTypes)
            .HasForeignKey(x => x.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}

public class ProductAttributeOptionConfiguration : IEntityTypeConfiguration<ProductAttributeOption>
{
    public void Configure(EntityTypeBuilder<ProductAttributeOption> b)
    {
        b.ToTable("ProductAttributeOptions");
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.Property(x => x.BarcodeCode).HasMaxLength(3);
        b.HasIndex(x => new { x.ProductAttributeTypeId, x.Code }).IsUnique();
        b.HasOne(x => x.ProductAttributeType)
            .WithMany(t => t.Options)
            .HasForeignKey(x => x.ProductAttributeTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
