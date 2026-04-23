using sfa_api.Features.NotBillings.Enums;

namespace sfa_api.Features.NotBillings.DTOs;

public record NotBillingDto(
    int Id,
    string NotBillingNumber,
    DateOnly NotBillingDate,

    // Core references
    int OutletId,
    string OutletName,
    int SalesRepId,
    string SalesRepName,

    // Org chain
    int? SupervisorUserId,
    string? SupervisorName,
    int? AsmUserId,
    string? AsmName,
    int? RsmUserId,
    string? RsmName,
    int? NsmUserId,
    string? NsmName,

    // Geo chain
    int? RouteId,
    int? DivisionId,
    int? TerritoryId,
    int? AreaId,
    int? RegionId,

    NotBillingReason Reason,
    string? Notes,
    DateTime CreatedAt
);
