namespace RetailCommerce.Application.DataManagement;

public record ImportRowError(int RowNumber, string? Sku, string Message);

public record ProductImportResultDto(int TotalRows, int Created, int Updated, int Failed, IReadOnlyList<ImportRowError> Errors);

public record InventoryImportResultDto(int TotalRows, int Applied, int Failed, IReadOnlyList<ImportRowError> Errors);

public record DiscountImportResultDto(int TotalRows, int Created, int Updated, int Failed, IReadOnlyList<ImportRowError> Errors);
