using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.ProductCategories.Requests;
using sfa_api.Features.ProductCategories.Services;

namespace sfa_api.Features.ProductCategories.Controllers;

[ApiController]
[Route("api/v1/product-categories")]
public class ProductCategoriesController(
    IProductCategoryService service,
    IValidator<CreateProductCategoryRequest> createValidator,
    IValidator<UpdateProductCategoryRequest> updateValidator) : ControllerBase
{
    private readonly IProductCategoryService _service = service;
    private readonly IValidator<CreateProductCategoryRequest> _createValidator = createValidator;
    private readonly IValidator<UpdateProductCategoryRequest> _updateValidator = updateValidator;

    /// <summary>
    /// GET /api/v1/product-categories/{id}
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetByIdAsync(id, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/product-categories
    /// </summary>
    [HttpGet]
    [Authorize]
    public async Task<IActionResult> GetAll(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string? search = null,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetAllAsync(page, pageSize, search, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/product-categories/all — lightweight dropdown list of all active categories
    /// </summary>
    [HttpGet("all")]
    [Authorize]
    public async Task<IActionResult> GetAllActive(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetAllActiveAsync(ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/product-categories
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create([FromBody] CreateProductCategoryRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        await _createValidator.ValidateOrThrowAsync(request, ct);
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.CreateAsync(request, callerId, ct);
        return CreatedAtAction(nameof(GetById), new { id = result.Id },
            ResponseHelper.Created(result, correlationId));
    }

    /// <summary>
    /// PUT /api/v1/product-categories/{id}
    /// </summary>
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Update(int id, [FromBody] UpdateProductCategoryRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        await _updateValidator.ValidateOrThrowAsync(request, ct);
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _service.UpdateAsync(id, request, callerId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/product-categories/{id}/activate
    /// </summary>
    [HttpPost("{id}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Activate(int id, CancellationToken ct)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        await _service.ActivateAsync(id, callerId, ct);
        return NoContent();
    }

    /// <summary>
    /// POST /api/v1/product-categories/{id}/deactivate
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Deactivate(int id, CancellationToken ct)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        await _service.DeactivateAsync(id, callerId, ct);
        return NoContent();
    }
}
