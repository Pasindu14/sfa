using sfa_api.Common.Extensions;
using sfa_api.Features.Stock.DTOs;
using sfa_api.Features.Stock.Enums;
using sfa_api.Features.Stock.Repositories;
using sfa_api.Features.Stock.Requests;
using sfa_api.Features.Stock.Validators;

namespace sfa_api.Features.Stock.Services;

/// <summary>Admin audit view over the stock ledger: who moved which stock, when, and why.</summary>
public class StockActivityService(IStockActivityRepository repo) : IStockActivityService
{
    private readonly IStockActivityRepository _repo = repo;

    public async Task<(List<StockActivityDto> Items, int TotalCount)> GetActivityAsync(
        StockActivityQuery query, CancellationToken ct = default)
    {
        // Inclusive business dates → half-open window [from 00:00 SL, to+1 00:00 SL), as UTC —
        // the same boundaries the bin card uses.
        var fromUtc        = SriLankaTime.StartOfDayUtc(query.From!.Value);
        var toExclusiveUtc = SriLankaTime.StartOfDayUtc(query.To!.Value.AddDays(1));

        StockTransactionType? type = string.IsNullOrWhiteSpace(query.TransactionType)
            ? null : Enum.Parse<StockTransactionType>(query.TransactionType, ignoreCase: true);
        StockTransactionDirection? direction = string.IsNullOrWhiteSpace(query.Direction)
            ? null : Enum.Parse<StockTransactionDirection>(query.Direction, ignoreCase: true);

        var (_, size, skip) = PaginationHelper.Normalize(query.Page, query.PageSize, StockActivityQueryValidator.MaxPageSize);

        var (items, total) = await _repo.GetPagedAsync(
            fromUtc, toExclusiveUtc, query.DistributorId, query.ProductId, query.UserId,
            type, direction, skip, size, ct);

        if (items.Count == 0)
            return (items, total);

        // Batched per reference type for the whole page — no per-row lookups.
        var numbers = await _repo.GetReferenceNumbersAsync(items.Select(i => (i.ReferenceType, i.ReferenceId)), ct);
        var resolved = items
            .Select(i => numbers.TryGetValue((i.ReferenceType, i.ReferenceId), out var n) ? i with { ReferenceNumber = n } : i)
            .ToList();

        return (resolved, total);
    }

    public Task<List<StockActivityUserDto>> GetUsersAsync(CancellationToken ct = default)
        => _repo.GetUsersAsync(ct);
}
