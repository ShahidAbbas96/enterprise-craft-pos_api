using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Common;
using RetailCommerce.Application.Sync;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/sync")]
[Authorize]
public class SyncController(ISyncService syncService) : ControllerBase
{
    [HttpPost("bootstrap")]
    public async Task<ActionResult<SyncSnapshotDto>> Bootstrap(CancellationToken ct) =>
        Ok(await syncService.BootstrapAsync(ct));

    [HttpGet("pull")]
    public async Task<ActionResult<SyncSnapshotDto>> Pull([FromQuery] DateTimeOffset? since, CancellationToken ct) =>
        Ok(await syncService.PullAsync(since, ct));

    [HttpGet("orders")]
    public async Task<ActionResult<IReadOnlyList<OrderSyncDto>>> Orders([FromQuery] DateTimeOffset? since, CancellationToken ct) =>
        Ok(await syncService.PullOrdersAsync(since, ct));

    [HttpGet("logs")]
    public async Task<ActionResult<PagedResult<SyncLogDto>>> Logs([FromQuery] SyncLogListQuery query, CancellationToken ct) =>
        Ok(await syncService.ListLogsAsync(query, ct));
}
