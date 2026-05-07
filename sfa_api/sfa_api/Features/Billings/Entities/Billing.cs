using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Billings.Enums;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Territories.Entities;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.Billings.Entities;

public class Billing
{
    public int Id { get; set; }
    public string BillingNumber { get; set; } = string.Empty;

    public DateOnly BillingDate { get; set; }

    // Core references
    public int OutletId { get; set; }
    public int SalesRepId { get; set; }         // from JWT at write time
    public int DistributorId { get; set; }       // denormalized at write time

    // Full org chain — all denormalized at write time from UserReportingLine
    public int? SupervisorUserId { get; set; }  // SalesRep's direct manager
    public int? AsmUserId { get; set; }         // Supervisor's manager
    public int? RsmUserId { get; set; }         // ASM's manager
    public int? NsmUserId { get; set; }         // RSM's manager

    // Full geo chain — all denormalized at write time from Outlet
    public int? RouteId { get; set; }
    public int? DivisionId { get; set; }
    public int? TerritoryId { get; set; }
    public int? AreaId { get; set; }
    public int? RegionId { get; set; }

    // Amounts
    public decimal SubTotalAmount { get; set; }      // Σ item.TotalPrice for Sale lines only
    public decimal BillDiscountRate { get; set; }    // Bill-level discount % (0–100), default 0
    public decimal BillDiscountAmount { get; set; }  // SubTotalAmount × BillDiscountRate / 100
    public decimal TotalAmount { get; set; }         // SubTotalAmount − BillDiscountAmount
    public decimal FreeIssueValue { get; set; }      // Σ item.TotalPrice for FreeIssue lines (informational, not in TotalAmount)

    public BillingStatus Status { get; set; } = BillingStatus.Submitted;
    public string? Notes { get; set; }

    // Location captured on the device at time of billing
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Outlet      Outlet      { get; set; } = null!;
    public User        SalesRep    { get; set; } = null!;
    public Distributor Distributor { get; set; } = null!;
    public Division?   Division    { get; set; }
    public Territory?  Territory   { get; set; }
    public Area?       Area        { get; set; }
    public Region?     Region      { get; set; }
    public User?       Supervisor  { get; set; }
    public User?       Asm         { get; set; }
    public User?       Rsm         { get; set; }
    public User?       Nsm         { get; set; }
    public ICollection<BillingItem> Items { get; set; } = [];
}
