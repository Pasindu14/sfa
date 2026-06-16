using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.PurchaseOrders.Enums;
using sfa_api.Features.PurchaseOrders.Requests;
using sfa_api.Features.PurchaseOrders.Services;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.PurchaseOrders.Controllers;

[ApiController]
[Route("api/v1/purchase-orders")]
[Authorize]
public class PurchaseOrdersController(
    IPurchaseOrderService purchaseOrderService,
    IValidator<CreatePurchaseOrderRequest> createValidator,
    IValidator<UpdatePurchaseOrderRequest> updateValidator) : ControllerBase
{
    private readonly IPurchaseOrderService _purchaseOrderService = purchaseOrderService;
    private readonly IValidator<CreatePurchaseOrderRequest> _createValidator = createValidator;
    private readonly IValidator<UpdatePurchaseOrderRequest> _updateValidator = updateValidator;

    // ── Helper to extract caller info from JWT ────────────────────────────

    private (int callerId, UserRole callerRole) GetCallerInfo()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var roleStr = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        Enum.TryParse<UserRole>(roleStr, out var callerRole);
        return (callerId, callerRole);
    }

    /// <summary>
    /// GET /api/v1/purchase-orders/stats
    /// Returns order counts grouped by status for the given date range.
    /// All roles — Distributor filtered to own orders.
    /// </summary>
    [HttpGet("stats")]
    public async Task<IActionResult> GetStats(
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _purchaseOrderService.GetStatsAsync(callerId, callerRole, fromDate, toDate, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/purchase-orders
    /// All roles — Distributor filtered to own orders
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        [FromQuery] DateOnly? fromDate = null,
        [FromQuery] DateOnly? toDate = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();

        PurchaseOrderStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<PurchaseOrderStatus>(status, true, out var parsed))
            statusFilter = parsed;

        var result = await _purchaseOrderService.GetAllAsync(
            page, pageSize, search, statusFilter, fromDate, toDate, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/purchase-orders/{id}
    /// All roles — Distributor: own only
    /// </summary>
    [HttpGet("{id:int}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _purchaseOrderService.GetByIdAsync(id, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/purchase-orders
    /// Distributor, Admin — creates in Draft
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Distributor,Admin")]
    [EnableRateLimiting("user")]
    public async Task<IActionResult> Create([FromBody] CreatePurchaseOrderRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _createValidator.ValidateOrThrowAsync(request, ct);

        var (callerId, callerRole) = GetCallerInfo();
        var result = await _purchaseOrderService.CreateAsync(request, callerId, callerRole, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ResponseHelper.Created(result, correlationId));
    }

    /// <summary>
    /// PUT /api/v1/purchase-orders/{id}
    /// Role + status gated (see edit rights table)
    /// </summary>
    [HttpPut("{id:int}")]
    [Authorize(Roles = "Distributor,SalesRep,Supervisor,Admin")]
    [EnableRateLimiting("user")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdatePurchaseOrderRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _updateValidator.ValidateOrThrowAsync(request, ct);

        var (callerId, callerRole) = GetCallerInfo();
        var result = await _purchaseOrderService.UpdateAsync(id, request, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/purchase-orders/{id}/submit
    /// Distributor, Admin — Status must be Draft
    /// </summary>
    [HttpPost("{id:int}/submit")]
    [Authorize(Roles = "Distributor,Admin")]
    public async Task<IActionResult> Submit(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _purchaseOrderService.SubmitAsync(id, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/purchase-orders/{id}/rep-approve
    /// SalesRep, Admin — Status must be PendingRepApproval
    /// </summary>
    [HttpPost("{id:int}/rep-approve")]
    [Authorize(Roles = "SalesRep,Admin")]
    public async Task<IActionResult> RepApprove(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _purchaseOrderService.RepApproveAsync(id, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/purchase-orders/{id}/approve
    /// Manager, Admin — Status must be PendingManagerApproval
    /// </summary>
    [HttpPost("{id:int}/approve")]
    [Authorize(Roles = "Supervisor,Admin")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _purchaseOrderService.ApproveAsync(id, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/purchase-orders/{id}/reject
    /// SalesRep/Manager/Admin — Role + status gated
    /// </summary>
    [HttpPost("{id:int}/reject")]
    [Authorize(Roles = "SalesRep,Supervisor,Admin")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectPurchaseOrderRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _purchaseOrderService.RejectAsync(id, request, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/purchase-orders/{id}/acknowledge
    /// Distributor, Admin — acknowledges a rejected order → Cancelled
    /// </summary>
    [HttpPost("{id:int}/acknowledge")]
    [Authorize(Roles = "Distributor,Admin")]
    public async Task<IActionResult> Acknowledge(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _purchaseOrderService.AcknowledgeAsync(id, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/purchase-orders/{id}/finalize
    /// Distributor, Admin — Status must be PendingDistributorFinalization
    /// </summary>
    [HttpPost("{id:int}/finalize")]
    [Authorize(Roles = "Distributor,Admin")]
    public async Task<IActionResult> Finalize(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _purchaseOrderService.FinalizeAsync(id, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/purchase-orders/{id}/cancel
    /// Distributor (Draft only), Admin
    /// </summary>
    [HttpPost("{id:int}/cancel")]
    [Authorize(Roles = "Distributor,Admin")]
    public async Task<IActionResult> Cancel(int id, [FromBody] RejectPurchaseOrderRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _purchaseOrderService.CancelAsync(id, request, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
