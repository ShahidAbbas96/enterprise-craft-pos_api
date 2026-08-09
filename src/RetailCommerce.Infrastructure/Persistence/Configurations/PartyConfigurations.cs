using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailCommerce.Domain.Parties;

namespace RetailCommerce.Infrastructure.Persistence.Configurations;

public class CustomerConfiguration : IEntityTypeConfiguration<Customer>
{
    public void Configure(EntityTypeBuilder<Customer> b)
    {
        b.ToTable("Customers");
        b.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
        b.Property(x => x.LastName).HasMaxLength(100);
        b.Property(x => x.Phone).HasMaxLength(30).IsRequired();
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.CreditLimit).HasPrecision(18, 2);
        b.Property(x => x.OpeningBalance).HasPrecision(18, 2);
        b.Property(x => x.Balance).HasPrecision(18, 2);
        b.Property(x => x.Type).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.HasIndex(x => x.Phone).IsUnique();
    }
}

public class SupplierConfiguration : IEntityTypeConfiguration<Supplier>
{
    public void Configure(EntityTypeBuilder<Supplier> b)
    {
        b.ToTable("Suppliers");
        b.Property(x => x.Name).HasMaxLength(200).IsRequired();
        b.Property(x => x.ContactName).HasMaxLength(150);
        b.Property(x => x.Email).HasMaxLength(200);
        b.Property(x => x.Phone).HasMaxLength(30);
        b.Property(x => x.Rating).HasPrecision(3, 2);
        b.Property(x => x.Balance).HasPrecision(18, 2);
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
    }
}

public class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> b)
    {
        b.ToTable("Employees");
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
    }
}
