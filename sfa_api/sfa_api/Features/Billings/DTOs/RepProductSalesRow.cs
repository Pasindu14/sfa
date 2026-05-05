namespace sfa_api.Features.Billings.DTOs;

/// <summary>Internal aggregation row used by the item-wise achievement endpoint.</summary>
public record RepProductSalesRow(int ProductId, decimal Qty, decimal Amount);
