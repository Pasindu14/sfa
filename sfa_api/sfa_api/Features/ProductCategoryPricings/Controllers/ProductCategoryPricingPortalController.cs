using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Features.ProductCategoryPricings.Services;
using sfa_api.Features.Users.Repositories;

namespace sfa_api.Features.ProductCategoryPricings.Controllers;

[ApiController]
[Route("api/v1/product-category-pricings")]
[Authorize(Roles = "Distributor")]
public class ProductCategoryPricingPortalController(
    IProductCategoryPricingService pricingService,
    IUserRepository userRepo) : ControllerBase
{
    private readonly IProductCategoryPricingService _pricingService = pricingService;
    private readonly IUserRepository _userRepo = userRepo;

    /// <summary>
    /// GET /api/v1/product-category-pricings/portal
    /// Distributor only — returns unit prices for ONLY the calling distributor's own
    /// category tier (resolved from the JWT), never every A/B/C/D tier. Used by the
    /// distributor portal create page to auto-fill unit prices.
    /// </summary>
    [HttpGet("portal")]
    public async Task<IActionResult> GetForPortal(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var user = await _userRepo.GetUserByIdAsync(userId, ct);
        if (user?.DistributorId == null)
            throw new BusinessRuleException("NO_DISTRIBUTOR_LINKED",
                "Your account is not linked to a distributor.");

        var result = await _pricingService.GetForDistributorAsync(user.DistributorId.Value, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
