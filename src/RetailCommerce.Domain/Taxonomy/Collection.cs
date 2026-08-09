using RetailCommerce.Domain.Common;

namespace RetailCommerce.Domain.Taxonomy;

/// <summary>Named merchandising drop/season, e.g. "Zebtan" V2 2024 → displayed as "Zebtan-V2-24".
/// Year is stored separately and is authoritative over any year encoded in the display code
/// (the client's source spreadsheet had at least one mismatch between the two).</summary>
public class Collection : BaseEntity
{
    public string Name { get; set; } = default!;
    public string VersionLabel { get; set; } = default!;
    public int Year { get; set; }
    public Guid? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public string DisplayCode => $"{Name}-{VersionLabel}-{Year % 100:D2}";
}
