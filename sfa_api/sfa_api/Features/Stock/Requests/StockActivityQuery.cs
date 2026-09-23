namespace sfa_api.Features.Stock.Requests;

/// <summary>
/// Query parameters for GET /api/v1/stock/activity. From/To are inclusive Sri Lanka business
/// dates; TransactionType / Direction are enum names ("TransferIn", "Out", …).
/// </summary>
public record StockActivityQuery(
    DateOnly? From,
    DateOnly? To,
    int?      DistributorId,
    int?      ProductId,
    int?      UserId,
    string?   TransactionType,
    string?   Direction,
    int       Page = 1,
    int       PageSize = 50);
