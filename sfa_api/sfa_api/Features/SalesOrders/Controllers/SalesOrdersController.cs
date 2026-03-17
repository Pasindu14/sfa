using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.SalesOrders.Enums;
using sfa_api.Features.SalesOrders.Requests;
using sfa_api.Features.SalesOrders.Services;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.SalesOrders.Controllers;

[ApiController]
[Route("api/v1/sales-orders")]
[Authorize]
public class SalesOrdersController(
    ISalesOrderService salesOrderService,
    IValidator<CreateSalesOrderRequest> createValidator,
    IValidator<UpdateSalesOrderRequest> updateValidator) : ControllerBase
{
    private readonly ISalesOrderService _salesOrderService = salesOrderService;
    private readonly IValidator<CreateSalesOrderRequest> _createValidator = createValidator;
    private readonly IValidator<UpdateSalesOrderRequest> _updateValidator = updateValidator;

    // ── Helper to extract caller info from JWT ────────────────────────────

    private (int callerId, UserRole callerRole) GetCallerInfo()
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var roleStr = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        Enum.TryParse<UserRole>(roleStr, out var callerRole);
        return (callerId, callerRole);
    }

    /// <summary>
    /// GET /api/v1/sales-orders
    /// All roles — Distributor filtered to own orders
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        [FromQuery] string? status = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();

        SalesOrderStatus? statusFilter = null;
        if (!string.IsNullOrWhiteSpace(status) && Enum.TryParse<SalesOrderStatus>(status, true, out var parsed))
            statusFilter = parsed;

        var result = await _salesOrderService.GetAllAsync(page, pageSize, search, statusFilter, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/sales-orders/{id}
    /// All roles — Distributor: own only
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _salesOrderService.GetByIdAsync(id, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/sales-orders
    /// Distributor, Admin — creates in Draft
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Distributor,Admin")]
    public async Task<IActionResult> Create([FromBody] CreateSalesOrderRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        var validation = await _createValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var fields = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Common.Errors.ValidationException(fields);
        }

        var (callerId, callerRole) = GetCallerInfo();
        var result = await _salesOrderService.CreateAsync(request, callerId, callerRole, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ResponseHelper.Created(result, correlationId));
    }

    /// <summary>
    /// PUT /api/v1/sales-orders/{id}
    /// Role + status gated (see edit rights table)
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Distributor,SalesRep,Manager,Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateSalesOrderRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        var validation = await _updateValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var fields = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Common.Errors.ValidationException(fields);
        }

        var (callerId, callerRole) = GetCallerInfo();
        var result = await _salesOrderService.UpdateAsync(id, request, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/sales-orders/{id}/submit
    /// Distributor, Admin — Status must be Draft
    /// </summary>
    [HttpPost("{id}/submit")]
    [Authorize(Roles = "Distributor,Admin")]
    public async Task<IActionResult> Submit(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _salesOrderService.SubmitAsync(id, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/sales-orders/{id}/rep-approve
    /// SalesRep, Admin — Status must be PendingRepApproval
    /// </summary>
    [HttpPost("{id}/rep-approve")]
    [Authorize(Roles = "SalesRep,Admin")]
    public async Task<IActionResult> RepApprove(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _salesOrderService.RepApproveAsync(id, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/sales-orders/{id}/approve
    /// Manager, Admin — Status must be PendingManagerApproval
    /// </summary>
    [HttpPost("{id}/approve")]
    [Authorize(Roles = "Manager,Admin")]
    public async Task<IActionResult> Approve(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _salesOrderService.ApproveAsync(id, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/sales-orders/{id}/reject
    /// SalesRep/Manager/Admin — Role + status gated
    /// </summary>
    [HttpPost("{id}/reject")]
    [Authorize(Roles = "SalesRep,Manager,Admin")]
    public async Task<IActionResult> Reject(int id, [FromBody] RejectSalesOrderRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _salesOrderService.RejectAsync(id, request, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/sales-orders/{id}/acknowledge
    /// Distributor, Admin — acknowledges a rejected order → Cancelled
    /// </summary>
    [HttpPost("{id}/acknowledge")]
    [Authorize(Roles = "Distributor,Admin")]
    public async Task<IActionResult> Acknowledge(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString();
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _salesOrderService.AcknowledgeAsync(id, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/sales-orders/{id}/finalize
    /// Distributor, Admin — Status must be PendingDistributorFinalization
    /// </summary>
    [HttpPost("{id}/finalize")]
    [Authorize(Roles = "Distributor,Admin")]
    public async Task<IActionResult> Finalize(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _salesOrderService.FinalizeAsync(id, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/sales-orders/{id}/cancel
    /// Distributor (Draft only), Admin
    /// </summary>
    [HttpPost("{id}/cancel")]
    [Authorize(Roles = "Distributor,Admin")]
    public async Task<IActionResult> Cancel(int id, [FromBody] RejectSalesOrderRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (callerId, callerRole) = GetCallerInfo();
        var result = await _salesOrderService.CancelAsync(id, request, callerId, callerRole, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
