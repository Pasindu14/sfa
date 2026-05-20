using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Features.Distributors.Services;
using sfa_api.Features.Users.Repositories;

namespace sfa_api.Features.Distributors.Controllers;

[ApiController]
[Route("api/v1/distributors")]
[Authorize(Roles = "Distributor")]
public class DistributorPortalController(
    IDistributorService distributorService,
    IUserRepository userRepo) : ControllerBase
{
    private readonly IDistributorService _distributorService = distributorService;
    private readonly IUserRepository _userRepo = userRepo;

    /// <summary>
    /// GET /api/v1/distributors/portal/profile
    /// Distributor only — returns own profile (id, name, category).
    /// DistributorId resolved from JWT sub claim; used by the portal create page for pricing tier.
    /// </summary>
    [HttpGet("portal/profile")]
    public async Task<IActionResult> GetMyProfile(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);
        var user = await _userRepo.GetUserByIdAsync(userId, ct);
        if (user?.DistributorId == null)
            throw new BusinessRuleException("NO_DISTRIBUTOR_LINKED",
                "Your account is not linked to a distributor.");
        var distributor = await _distributorService.GetByIdAsync(user.DistributorId.Value, ct);
        return Ok(ResponseHelper.Ok(distributor, correlationId));
    }
}
