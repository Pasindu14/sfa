namespace sfa_api.Features.Stock.Enums;

/// <summary>Why an admin changed a distributor's stock balance by hand. Stored as a string.</summary>
public enum StockAdjustmentReason
{
    Damage          = 0,
    Expiry          = 1,
    CountCorrection = 2,
    DataEntryError  = 3,
    Other           = 4
}
