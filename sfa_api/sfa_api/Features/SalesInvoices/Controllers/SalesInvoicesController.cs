using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.SalesInvoices.Requests;
using sfa_api.Features.SalesInvoices.Services;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.SalesInvoices.Controllers;

[ApiController]
[Route("api/v1/sales-invoices")]
[Authorize]
public class SalesInvoicesController(
    ISalesInvoiceService salesInvoiceService,
    IValidator<ImportSalesInvoicesRequest> importValidator) : ControllerBase
{
    private readonly ISalesInvoiceService _salesInvoiceService = salesInvoiceService;
    private readonly IValidator<ImportSalesInvoicesRequest> _importValidator = importValidator;

    private int GetCallerId()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        return callerId;
    }

    private (int callerId, UserRole callerRole) GetCallerInfo()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        Enum.TryParse<UserRole>(User.FindFirstValue(ClaimTypes.Role) ?? string.Empty, out var callerRole);
        return (callerId, callerRole);
    }

    /// <summary>
    /// GET /api/v1/sales-invoices
    /// Returns a paginated list with optional search and status filter.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetList(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] string? dateFrom = null,
        [FromQuery] string? dateTo = null,
        [FromQuery] int? distributorId = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        DateOnly? parsedDateFrom = DateOnly.TryParse(dateFrom, out var df) ? df : null;
        DateOnly? parsedDateTo = DateOnly.TryParse(dateTo, out var dt) ? dt : null;
        var (callerId, callerRole) = GetCallerInfo();
        var (items, total) = await _salesInvoiceService.GetListAsync(
            page, pageSize, search, status, parsedDateFrom, parsedDateTo, distributorId, callerId, callerRole, ct);
        return Ok(ResponseHelper.Paged(items, page, pageSize, total, correlationId));
    }

    /// <summary>
    /// GET /api/v1/sales-invoices/{id}
    /// Returns full detail for a single invoice including line items.
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();
        var invoice = await _salesInvoiceService.GetDetailAsync(id, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(invoice, correlationId));
    }

    /// <summary>
    /// POST /api/v1/sales-invoices/import
    /// Admin only — imports an Excel-parsed batch of sales invoices from BUSY ERP.
    /// </summary>
    [HttpPost("import")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Import([FromBody] ImportSalesInvoicesRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _importValidator.ValidateOrThrowAsync(request, ct);

        var callerId = GetCallerId();
        var result = await _salesInvoiceService.ImportAsync(request, callerId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
