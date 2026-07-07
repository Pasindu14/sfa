using Microsoft.EntityFrameworkCore;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.NotBillings.DTOs;
using sfa_api.Features.NotBillings.Entities;
using sfa_api.Features.NotBillings.Enums;
using sfa_api.Features.NotBillings.Repositories;
using sfa_api.Features.NotBillings.Requests;
using sfa_api.Features.UserGeoAssignments.Repositories;
using sfa_api.Features.UserReportingLines.Repositories;

namespace sfa_api.Features.NotBillings.Services;

public class NotBillingService(
    INotBillingRepository notBillingRepository,
    IUserGeoAssignmentRepository geoAssignmentRepository,
    IUserReportingLineRepository reportingLineRepository) : INotBillingService
{
    private readonly INotBillingRepository _notBillingRepository = notBillingRepository;
    private readonly IUserGeoAssignmentRepository _geoAssignmentRepository = geoAssignmentRepository;
    private readonly IUserReportingLineRepository _reportingLineRepository = reportingLineRepository;

    public async Task<NotBillingDto> CreateAsync(CreateNotBillingRequest request, int salesRepId, string? clientRecordId = null, CancellationToken ct = default)
    {
        // ⓪ Idempotent fast-path — a retry of a submission that already succeeded returns the
        // existing record without redoing work or burning a not-billing-number sequence value.
        // Unlike the (rep, outlet, date) natural key, this survives a retry that crosses midnight
        // or arrives after the idempotency-cache TTL (finding #6).
        if (!string.IsNullOrWhiteSpace(clientRecordId))
        {
            var existingId = await _notBillingRepository.FindIdByClientRecordIdAsync(clientRecordId, ct);
            if (existingId is not null)
            {
                var existing = await _notBillingRepository.GetByIdAsync(existingId.Value, ct)
                    ?? throw new DatabaseUnavailableException();
                return ProjectToDto(existing);
            }
        }

        // ① Validate outlet
        var outlet = await _notBillingRepository.GetOutletAsync(request.OutletId, ct)
            ?? throw new NotFoundException("Outlet", request.OutletId);

        var date = request.NotBillingDate ?? SriLankaTime.Today;

        // ② Prevent duplicate not-billing for same rep + outlet + day
        var exists = await _notBillingRepository.ExistsForOutletTodayAsync(salesRepId, request.OutletId, date, ct);
        if (exists)
            throw new BusinessRuleException(
                "DUPLICATE_NOT_BILLING",
                $"A not-billing record already exists for outlet {request.OutletId} on {date:yyyy-MM-dd}.");

        // ③ Resolve geo from UserGeoAssignment
        var geo = await _geoAssignmentRepository.GetActiveByUserIdAsync(salesRepId, ct)
            ?? throw new BusinessRuleException(
                "GEO_ASSIGNMENT_NOT_FOUND",
                $"Sales rep {salesRepId} has no active geographic assignment.");

        // ④ Walk org chain — 4 hops up UserReportingLine
        var l1 = await _reportingLineRepository.GetActiveByUserIdAsync(salesRepId, ct);
        int? supervisorId = l1?.ReportsToUserId;

        var l2 = supervisorId.HasValue
            ? await _reportingLineRepository.GetActiveByUserIdAsync(supervisorId.Value, ct) : null;
        int? asmId = l2?.ReportsToUserId;

        var l3 = asmId.HasValue
            ? await _reportingLineRepository.GetActiveByUserIdAsync(asmId.Value, ct) : null;
        int? rsmId = l3?.ReportsToUserId;

        var l4 = rsmId.HasValue
            ? await _reportingLineRepository.GetActiveByUserIdAsync(rsmId.Value, ct) : null;
        int? nsmId = l4?.ReportsToUserId;

        // ⑤ Generate number
        var seqNo = await _notBillingRepository.GetNextNotBillingNumberAsync(ct);
        var notBillingNumber = $"NBL-{SriLankaTime.Year}-{seqNo:D5}";

        // ⑥ Build entity with full org + geo chain
        var notBilling = new NotBilling
        {
            NotBillingNumber  = notBillingNumber,
            ClientRecordId    = string.IsNullOrWhiteSpace(clientRecordId) ? null : clientRecordId,
            NotBillingDate    = date,
            OutletId          = request.OutletId,
            SalesRepId        = salesRepId,
            Reason            = request.Reason,
            Notes             = request.Notes,
            SupervisorUserId  = supervisorId,
            AsmUserId         = asmId,
            RsmUserId         = rsmId,
            NsmUserId         = nsmId,
            RouteId           = outlet.RouteId,
            DivisionId        = outlet.DivisionId,
            TerritoryId       = geo.TerritoryId,
            AreaId            = geo.AreaId,
            RegionId          = geo.RegionId,
            CreatedAt         = DateTime.UtcNow,
            UpdatedAt         = DateTime.UtcNow,
            CreatedBy         = salesRepId
        };

        await _notBillingRepository.AddAsync(notBilling, ct);
        try
        {
            await _notBillingRepository.SaveChangesAsync(ct);
        }
        catch (DbUpdateException) when (!string.IsNullOrWhiteSpace(clientRecordId))
        {
            // A concurrent request with the same client record id won the insert race; the unique
            // index rejected ours. Return the winner so this retry is an idempotent success.
            var winnerId = await _notBillingRepository.FindIdByClientRecordIdAsync(clientRecordId, ct);
            if (winnerId is null) throw;
            var winner = await _notBillingRepository.GetByIdAsync(winnerId.Value, ct)
                ?? throw new DatabaseUnavailableException();
            return ProjectToDto(winner);
        }

        // ⑦ Re-fetch read-only for DTO projection
        var created = await _notBillingRepository.GetByIdAsync(notBilling.Id, ct)
            ?? throw new DatabaseUnavailableException();

        return ProjectToDto(created);
    }

    public async Task<NotBillingDto?> GetByIdAsync(int id, CancellationToken ct = default)
    {
        var notBilling = await _notBillingRepository.GetByIdAsync(id, ct);
        return notBilling is null ? null : ProjectToDto(notBilling);
    }

    public Task<(List<NotBillingListDto> Items, int TotalCount)> GetListAsync(
        int page, int pageSize,
        int? outletId, int? salesRepId,
        NotBillingReason? reason,
        DateOnly? dateFrom, DateOnly? dateTo,
        CancellationToken ct = default)
        => _notBillingRepository.GetListAsync(page, pageSize, outletId, salesRepId, reason, dateFrom, dateTo, ct);

    private static NotBillingDto ProjectToDto(NotBilling n) => new(
        n.Id,
        n.NotBillingNumber,
        n.NotBillingDate,
        n.OutletId,
        n.Outlet?.Name ?? string.Empty,
        n.SalesRepId,
        n.SalesRep?.Name ?? string.Empty,
        n.SupervisorUserId,
        n.Supervisor?.Name,
        n.AsmUserId,
        n.Asm?.Name,
        n.RsmUserId,
        n.Rsm?.Name,
        n.NsmUserId,
        n.Nsm?.Name,
        n.RouteId,
        n.DivisionId,
        n.TerritoryId,
        n.AreaId,
        n.RegionId,
        n.Reason,
        n.Notes,
        n.CreatedAt
    );
}
