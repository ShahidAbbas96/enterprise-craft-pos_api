namespace RetailCommerce.Application.PosTerminals;

/// <summary>Admin CRUD for physical POS tills — each assigned to exactly one Warehouse (whose
/// Store the terminal transitively belongs to, see PosTerminal's doc comment) and one or more
/// login users. TokenService bakes an assigned user's terminal into their JWT at login/select-
/// terminal, so every POS-runtime request is server-scoped without the client naming a store.</summary>
public interface IPosTerminalService
{
    Task<IReadOnlyList<PosTerminalDto>> ListAsync(Guid? storeId = null, CancellationToken ct = default);
    Task<PosTerminalDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<PosTerminalDto> CreateAsync(UpsertPosTerminalRequest request, CancellationToken ct = default);
    Task<PosTerminalDto> UpdateAsync(Guid id, UpsertPosTerminalRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}
