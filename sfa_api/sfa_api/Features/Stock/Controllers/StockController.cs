using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using sfa_api.Common.Errors;
using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Repositories;

namespace sfa_api.Features.Stock.Controllers;

[ApiController]
[Route("api/v1/stock")]
[Authorize]
public class StockController(IStockRepository stockRepository) : ControllerBase
{
    private readonly IStockRepository _stockRepository = stockRepository;

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
