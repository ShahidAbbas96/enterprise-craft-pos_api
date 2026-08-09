namespace RetailCommerce.Application.Taxonomy;

public interface ITaxonomyService
{
    Task<TaxonomySnapshotDto> GetSnapshotAsync(CancellationToken ct = default);
}
