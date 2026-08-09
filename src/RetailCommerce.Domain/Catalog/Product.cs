using RetailCommerce.Domain.Common;
using RetailCommerce.Domain.Parties;
using RetailCommerce.Domain.Taxonomy;

namespace RetailCommerce.Domain.Catalog;

public class Product : BaseEntity
{
    /// <summary>Client business code, e.g. "F24625001". Free text on purpose — the client's
    /// generation algorithm is not fully confirmed (see CLAUDE.md §5.3), so this is editable/
    /// importable rather than auto-derived from the taxonomy below.</summary>
    public string? ItemCode { get; set; }

    public string Sku { get; set; } = default!;
    public string? Barcode { get; set; }
    public string Name { get; set; } = default!;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }

    public Guid DepartmentId { get; set; }
    public Department Department { get; set; } = default!;

    public Guid GenderId { get; set; }
    public Gender Gender { get; set; } = default!;

    public Guid EventTypeId { get; set; }
    public EventType EventType { get; set; } = default!;

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = default!;

    public Guid? SubcategoryId { get; set; }
    public Subcategory? Subcategory { get; set; }

    public Guid? CollectionId { get; set; }
    public Collection? Collection { get; set; }

    /// <summary>Authoritative year for the item, independent of any year encoded in the
    /// Collection's display code (the client's source data had at least one mismatch).</summary>
    public int? Year { get; set; }

    public Guid? SupplierId { get; set; }
    public Supplier? Supplier { get; set; }

    public decimal Cost { get; set; }
    public decimal Price { get; set; }
    public decimal? WholesalePrice { get; set; }
    public decimal TaxRatePercent { get; set; }
    public decimal DiscountPercent { get; set; }
    public string Unit { get; set; } = "pc";

    public int MinStock { get; set; }
    public int? MaxStock { get; set; }
    public int ReorderLevel { get; set; } = 10;

    public string? Location { get; set; }
    public ProductStatus Status { get; set; } = ProductStatus.Active;

    public ICollection<ProductAttributeValue> AttributeValues { get; set; } = new List<ProductAttributeValue>();
    public ICollection<ProductBarcode> Barcodes { get; set; } = new List<ProductBarcode>();
}
