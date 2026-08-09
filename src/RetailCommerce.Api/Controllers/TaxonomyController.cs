using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RetailCommerce.Application.Taxonomy;

namespace RetailCommerce.Api.Controllers;

[ApiController]
[Route("api/taxonomy")]
[Authorize]
public class TaxonomyController(ITaxonomyService taxonomyService) : ControllerBase
{
    /// <summary>Everything the Add/Edit Product form needs to drive its cascading
    /// Department → Gender → Event → Category → Subcategory selects and the department-aware
    /// attribute panel, in one call.</summary>
    [HttpGet("snapshot")]
    public async Task<ActionResult<TaxonomySnapshotDto>> Snapshot(CancellationToken ct)
    {
        return Ok(await taxonomyService.GetSnapshotAsync(ct));
    }
}
