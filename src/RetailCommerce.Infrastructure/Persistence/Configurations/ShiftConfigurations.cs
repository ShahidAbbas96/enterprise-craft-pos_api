using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RetailCommerce.Domain.Shifts;

namespace RetailCommerce.Infrastructure.Persistence.Configurations;

public class ExpenseCategoryConfiguration : IEntityTypeConfiguration<ExpenseCategory>
{
    public void Configure(EntityTypeBuilder<ExpenseCategory> b)
    {
        b.ToTable("ExpenseCategories");
        b.Property(x => x.Code).HasMaxLength(50).IsRequired();
        b.Property(x => x.Name).HasMaxLength(100).IsRequired();
        b.HasIndex(x => x.Code).IsUnique();
    }
}

public class ShiftConfiguration : IEntityTypeConfiguration<Shift>
{
    public void Configure(EntityTypeBuilder<Shift> b)
    {
        b.ToTable("Shifts");
        b.Property(x => x.ShiftNumber).HasMaxLength(30).IsRequired();
        b.HasIndex(x => x.ShiftNumber).IsUnique();
        b.Property(x => x.Status).HasConversion<string>().HasMaxLength(20);
        b.Property(x => x.TotalSales).HasPrecision(18, 2);
        b.Property(x => x.TotalExpenses).HasPrecision(18, 2);
        b.Property(x => x.NetTotal).HasPrecision(18, 2);

        b.HasOne(x => x.Warehouse).WithMany().HasForeignKey(x => x.WarehouseId).OnDelete(DeleteBehavior.Restrict);

        // Only one Open shift per warehouse at a time — enforced with a partial unique index
        // rather than application-level locking, so it holds even under concurrent requests.
        b.HasIndex(x => x.WarehouseId)
            .IsUnique()
            .HasFilter("\"Status\" = 'Open'")
            .HasDatabaseName("IX_Shifts_WarehouseId_OpenOnly");
    }
}

public class ExpenseConfiguration : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> b)
    {
        b.ToTable("Expenses");
        b.Property(x => x.Amount).HasPrecision(18, 2);
        b.Property(x => x.Note).HasMaxLength(300);

        b.HasOne(x => x.Shift).WithMany(s => s.Expenses).HasForeignKey(x => x.ShiftId).OnDelete(DeleteBehavior.Cascade);
        b.HasOne(x => x.ExpenseCategory).WithMany().HasForeignKey(x => x.ExpenseCategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}
