namespace sfa_api.Features.Billings.Enums;

public enum ReturnType
{
    MarketResell = 0,  // Stock credited back IN to distributor
    Damage       = 1,  // Write-off — no stock movement
    Expire       = 2   // Write-off — no stock movement
}
