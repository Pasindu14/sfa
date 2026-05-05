using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.SalesTargets.Requests;
using sfa_api.Features.SalesTargets.Services;

namespace sfa_api.Features.SalesTargets.Controllers;

[ApiController]
[Route("api/v1/sales-targets")]
[Authorize(Roles = "Admin")]
public class SalesTargetsController(
    ISalesTargetService targetService,
    ISalesTargetImportService importService,
    IValidator<ImportSalesTargetsRequest> importValidator) : ControllerBase
{
    private int GetCallerId()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id);
        return id;
    }

    /// <summary>GET /api/v1/sales-targets — paged target list with optional filters.</summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int  page       = 1,
        [FromQuery] int  pageSize   = 20,
        [FromQuery] int? year       = null,
        [FromQuery] int? month      = null,
        [FromQuery] int? salesRepId = null,
        [FromQuery] int? productId  = null,
        [FromQuery] string? search  = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (items, total) = await targetService.GetPagedAsync(
            page, pageSize, year, month, salesRepId, productId, search, ct);
        return Ok(ResponseHelper.Paged(items, page, pageSize, total, correlationId));
    }

    /// <summary>POST /api/v1/sales-targets/import — bulk upsert from parsed Excel JSON.</summary>
    [HttpPost("import")]
    public async Task<IActionResult> Import(
        [FromBody] ImportSalesTargetsRequest request,
        CancellationToken ct)
    {
        await importValidator.ValidateOrThrowAsync(request, ct);
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await importService.ImportAsync(request, GetCallerId(), ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>GET /api/v1/sales-targets/import-batches — paged import history.</summary>
    [HttpGet("import-batches")]
    public async Task<IActionResult> GetBatches(
        [FromQuery] int page     = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (items, total) = await targetService.GetBatchesPagedAsync(page, pageSize, ct);
        return Ok(ResponseHelper.Paged(items, page, pageSize, total, correlationId));
    }

    /// <summary>GET /api/v1/sales-targets/import-batches/{id} — single batch detail.</summary>
    [HttpGet("import-batches/{id:int}")]
    public async Task<IActionResult> GetBatchById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var batch = await targetService.GetBatchByIdAsync(id, ct);
        if (batch is null)
            throw new NotFoundException("ImportBatch", id);
        return Ok(ResponseHelper.Ok(batch, correlationId));
    }
}
