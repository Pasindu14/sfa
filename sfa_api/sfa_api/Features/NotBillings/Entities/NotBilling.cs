using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.NotBillings.Enums;
using sfa_api.Features.Outlets.Entities;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Territories.Entities;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.NotBillings.Entities;

public class NotBilling
{
    public int Id { get; set; }
    public string NotBillingNumber { get; set; } = string.Empty;

    public DateOnly NotBillingDate { get; set; }

    // Core references
    public int OutletId { get; set; }
    public int SalesRepId { get; set; }       // from JWT at write time

    // Reason for not billing
    public NotBillingReason Reason { get; set; }
    public string? Notes { get; set; }

    // Full org chain — all denormalized at write time from UserReportingLine
    public int? SupervisorUserId { get; set; }
    public int? AsmUserId { get; set; }
    public int? RsmUserId { get; set; }
    public int? NsmUserId { get; set; }

    // Full geo chain — all denormalized at write time from Outlet + UserGeoAssignment
    public int? RouteId { get; set; }
    public int? DivisionId { get; set; }
    public int? TerritoryId { get; set; }
    public int? AreaId { get; set; }
    public int? RegionId { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public Outlet      Outlet     { get; set; } = null!;
    public User        SalesRep   { get; set; } = null!;
    public Division?   Division   { get; set; }
    public Territory?  Territory  { get; set; }
    public Area?       Area       { get; set; }
    public Region?     Region     { get; set; }
    public User?       Supervisor { get; set; }
    public User?       Asm        { get; set; }
    public User?       Rsm        { get; set; }
    public User?       Nsm        { get; set; }
}
