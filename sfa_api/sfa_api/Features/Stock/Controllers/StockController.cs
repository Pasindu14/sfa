using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Distributors.Repositories;
using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Features.Stock.Requests;
using sfa_api.Features.Stock.Services;
using sfa_api.Features.UserGeoAssignments.Repositories;
using sfa_api.Features.Users.Repositories;

namespace sfa_api.Features.Stock.Controllers;

[ApiController]
[Route("api/v1/stock")]
[Authorize]
public class StockController(
    IStockRepository stockRepository,
    IUserGeoAssignmentRepository geoRepo,
    IDistributorRepository distributorRepo,
    IUserRepository userRepo,
    IBinCardService binCardService,
    IValidator<BinCardQuery> binCardValidator) : ControllerBase
{
    private readonly IStockRepository _stockRepository = stockRepository;
    private readonly IUserGeoAssignmentRepository _geoRepo = geoRepo;
    private readonly IDistributorRepository _distributorRepo = distributorRepo;
    private readonly IUserRepository _userRepo = userRepo;
    private readonly IBinCardService _binCardService = binCardService;
    private readonly IValidator<BinCardQuery> _binCardValidator = binCardValidator;

    /// <summary>
    /// GET /api/v1/stock/my-distributor
    /// Returns all stock levels for the calling rep's assigned distributor (mobile sync endpoint).
    /// Resolves the distributor via the rep's active geo assignment → territory → distributor.
    /// Returns 400 if the calling user has no active geo assignment or territory distributor.
    /// </summary>
    [HttpGet("my-distributor")]
    public async Task<IActionResult> GetMyDistributorStock(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);

        var geo = await _geoRepo.GetActiveWithDetailsByUserIdAsync(userId, ct);
        if (geo?.TerritoryId == null)
            throw new BusinessRuleException("NO_DISTRIBUTOR_ASSIGNED",
                "The calling user has no active geo assignment with a territory.");

        var distributor = await _distributorRepo.GetByTerritoryIdAsync(geo.TerritoryId.Value, ct);
        if (distributor == null)
            throw new BusinessRuleException("NO_DISTRIBUTOR_ASSIGNED",
                "No distributor is assigned to the calling user's territory.");

        var stocks = await _stockRepository.GetAllStockByDistributorAsync(distributor.Id, ct);
        var dtos = stocks.Select(s => new DistributorStockDto(
            s.Id,
            s.DistributorId,
            s.Distributor?.Name ?? string.Empty,
            s.ProductId,
            s.Product?.Code ?? string.Empty,
            s.Product?.ItemDescription ?? string.Empty,
            s.StockType.ToString(),
            s.QuantityOnHand,
            s.LastUpdatedAt,
            s.FleetId,
            s.Fleet?.Name
        )).ToList();

        return Ok(ResponseHelper.Ok(dtos, correlationId));
    }

    /// <summary>
    /// GET /api/v1/stock/distributors/{distributorId}
    /// Returns all current stock levels for a given distributor.
    /// </summary>
    [HttpGet("distributors/{distributorId:int}")]
    [Authorize(Roles = "Admin,NSM,RSM,ASM")]
    public async Task<IActionResult> GetByDistributor(
        int distributorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var (page2, pageSize2, skip) = PaginationHelper.Normalize(page, pageSize);
        page = page2; pageSize = pageSize2;
        var (stocks, total) = await _stockRepository.GetStockByDistributorAsync(distributorId, skip, pageSize, ct);
        var dtos = stocks.Select(s => new DistributorStockDto(
            s.Id,
            s.DistributorId,
            s.Distributor?.Name ?? string.Empty,
            s.ProductId,
            s.Product?.Code ?? string.Empty,
            s.Product?.ItemDescription ?? string.Empty,
            s.StockType.ToString(),
            s.QuantityOnHand,
            s.LastUpdatedAt,
            s.FleetId,
            s.Fleet?.Name
        )).ToList();
        return Ok(ResponseHelper.Paged(dtos, page, pageSize, total, correlationId));
    }

    /// <summary>
    /// GET /api/v1/stock/portal
    /// Returns all stock levels for the currently logged-in Distributor user.
    /// Resolves the distributor from the JWT sub claim → User.DistributorId.
    /// </summary>
    [HttpGet("portal")]
    [Authorize(Roles = "Distributor")]
    public async Task<IActionResult> GetPortalStock(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var userId);

        var user = await _userRepo.GetUserByIdAsync(userId, ct);
        if (user?.DistributorId == null)
            throw new BusinessRuleException("NO_DISTRIBUTOR_LINKED",
                "Your account is not linked to a distributor.");

        var stocks = await _stockRepository.GetAllStockByDistributorAsync(user.DistributorId.Value, ct);
        var dtos = stocks.Select(s => new DistributorStockDto(
            s.Id,
            s.DistributorId,
            s.Distributor?.Name ?? string.Empty,
            s.ProductId,
            s.Product?.Code ?? string.Empty,
            s.Product?.ItemDescription ?? string.Empty,
            s.StockType.ToString(),
            s.QuantityOnHand,
            s.LastUpdatedAt,
            s.FleetId,
            s.Fleet?.Name
        )).ToList();

        return Ok(ResponseHelper.Ok(dtos, correlationId));
    }

    /// <summary>
    /// GET /api/v1/stock/distributors/{distributorId}/products/{productId}/transactions
    /// Returns paginated stock transaction history for a distributor+product combination.
    /// </summary>
    [HttpGet("distributors/{distributorId:int}/products/{productId:int}/transactions")]
    [Authorize(Roles = "Admin,NSM,RSM,ASM")]
    public async Task<IActionResult> GetTransactions(
        int distributorId, int productId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var total = await _stockRepository.GetTransactionCountAsync(distributorId, productId, ct);
        var txs = await _stockRepository.GetTransactionsByDistributorAndProductAsync(distributorId, productId, page, pageSize, ct);
        var dtos = txs.Select(t => new StockTransactionDto(
            t.Id,
            t.ProductId,
            t.Product?.Code ?? string.Empty,
            t.TransactionType.ToString(),
            t.Direction.ToString(),
            t.Quantity,
            t.QuantityBefore,
            t.QuantityAfter,
            t.ReferenceType,
            t.ReferenceId,
            t.TransactedAt,
            t.Notes
        )).ToList();
        return Ok(ResponseHelper.Paged(dtos, page, pageSize, total, correlationId));
    }

    /// <summary>
    /// GET /api/v1/stock/distributors/{distributorId}/bin-card?from=YYYY-MM-DD&amp;to=YYYY-MM-DD
    /// Per-SKU bin card for a distributor over a date range: opening stock, every movement
    /// (receipts, returns, reversals, adjustments, sales, free issues), end stock, latest
    /// physical count, closing value and variance — plus grand totals.
    /// </summary>
    [HttpGet("distributors/{distributorId:int}/bin-card")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetBinCard(
        int distributorId,
        [FromQuery] DateOnly from,
        [FromQuery] DateOnly to,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var query = new BinCardQuery(distributorId, from, to);
        await _binCardValidator.ValidateOrThrowAsync(query, ct);
        var result = await _binCardService.GetBinCardAsync(query, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }
}
