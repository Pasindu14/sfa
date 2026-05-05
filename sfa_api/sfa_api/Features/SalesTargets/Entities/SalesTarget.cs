using sfa_api.Features.Areas.Entities;
using sfa_api.Features.Distributors.Entities;
using sfa_api.Features.Divisions.Entities;
using sfa_api.Features.Products.Entities;
using sfa_api.Features.Regions.Entities;
using sfa_api.Features.Territories.Entities;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.SalesTargets.Entities;

public class SalesTarget
{
    public int Id { get; set; }
    public int ImportBatchId { get; set; }

    public int Year  { get; set; }
    public int Month { get; set; }

    public int SalesRepId { get; set; }
    public int ProductId  { get; set; }

    public decimal TargetQuantity { get; set; }

    // Denormalized org chain — written once at import time from UserReportingLine (4 hops)
    public int? SupervisorUserId { get; set; }
    public int? AsmUserId        { get; set; }
    public int? RsmUserId        { get; set; }
    public int? NsmUserId        { get; set; }

    // Denormalized geo chain — written once at import time from UserGeoAssignment
    public int? DistributorId { get; set; }
    public int? DivisionId    { get; set; }
    public int? TerritoryId   { get; set; }
    public int? AreaId        { get; set; }
    public int? RegionId      { get; set; }

    // Audit
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public int? CreatedBy { get; set; }
    public int? UpdatedBy { get; set; }
    public bool IsActive  { get; set; } = true;
    public bool IsDeleted { get; set; } = false;

    // Navigation
    public User?        SalesRep    { get; set; }
    public Product?     Product     { get; set; }
    public Distributor? Distributor { get; set; }
    public Division?    Division    { get; set; }
    public Territory?   Territory   { get; set; }
    public Area?        Area        { get; set; }
    public Region?      Region      { get; set; }
    public User?        Supervisor  { get; set; }
    public User?        Asm         { get; set; }
    public User?        Rsm         { get; set; }
    public User?        Nsm         { get; set; }
    public SalesTargetImportBatch? ImportBatch { get; set; }
}
