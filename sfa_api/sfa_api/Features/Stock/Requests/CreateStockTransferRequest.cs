using sfa_api.Features.Stock.Enums;

namespace sfa_api.Features.Stock.Requests;

/// <summary>POST /api/v1/stock-transfers — StockType binds from "Normal" / "FreeIssue".</summary>
public record CreateStockTransferRequest(
    int SourceDistributorId,
    int TargetDistributorId,
    string? Notes,
    List<CreateStockTransferLineRequest> Lines);

/// <summary>Quantity is in pieces.</summary>
public record CreateStockTransferLineRequest(
    int ProductId,
    StockType StockType,
    decimal Quantity);
