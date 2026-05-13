using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Features.Distributors.Repositories;
using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Features.UserGeoAssignments.Repositories;

namespace sfa_api.Features.Stock.Controllers;

[ApiController]
[Route("api/v1/stock")]
[Authorize]
public class StockController(
    IStockRepository stockRepository,
    IUserGeoAssignmentRepository geoRepo,
    IDistributorRepository distributorRepo) : ControllerBase
{
    private readonly IStockRepository _stockRepository = stockRepository;
    private readonly IUserGeoAssignmentRepository _geoRepo = geoRepo;
    private readonly IDistributorRepository _distributorRepo = distributorRepo;

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
            s.LastUpdatedAt
        )).ToList();

        return Ok(ResponseHelper.Ok(dtos, correlationId));
    }

    /// <summary>
    /// GET /api/v1/stock/distributors/{distributorId}
    /// Returns all current stock levels for a given distributor.
    /// </summary>
    [HttpGet("distributors/{distributorId:int}")]
    public async Task<IActionResult> GetByDistributor(
        int distributorId,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 50,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var skip = (page - 1) * pageSize;
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
            s.LastUpdatedAt
        )).ToList();
        return Ok(ResponseHelper.Paged(dtos, page, pageSize, total, correlationId));
    }

    /// <summary>
    /// GET /api/v1/stock/distributors/{distributorId}/products/{productId}/transactions
    /// Returns paginated stock transaction history for a distributor+product combination.
    /// </summary>
    [HttpGet("distributors/{distributorId:int}/products/{productId:int}/transactions")]
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
}
