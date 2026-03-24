using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.SalesInvoices.Requests;
using sfa_api.Features.SalesInvoices.Services;

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
