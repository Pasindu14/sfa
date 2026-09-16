using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.UserProximityExemptions.DTOs;
using sfa_api.Features.UserProximityExemptions.Entities;
using sfa_api.Features.UserProximityExemptions.Repositories;
using sfa_api.Features.UserProximityExemptions.Requests;
using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.UserProximityExemptions.Services;

public class UserProximityExemptionService(
    IUserProximityExemptionRepository repo,
    ILogger<UserProximityExemptionService> logger) : IUserProximityExemptionService
{
    private readonly IUserProximityExemptionRepository _repo = repo;
    private readonly ILogger<UserProximityExemptionService> _logger = logger;

    public async Task<UserProximityExemptionDto> GrantAsync(
        int userId, GrantProximityExemptionRequest request, int callerId, CancellationToken ct = default)
    {
        var user = await _repo.GetUserAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        // The geofence only ever runs on rep bill submission, so an exemption on
        // any other role would be a no-op that reads like a real control.
        if (user.Role != UserRole.SalesRep)
            throw new BusinessRuleException(
                "PROXIMITY_EXEMPTION_ROLE_INVALID",
                "Proximity exemptions apply only to sales reps - the geofence is not enforced for other roles.");

        if (!user.IsActive)
            throw new BusinessRuleException(
                "PROXIMITY_EXEMPTION_USER_INACTIVE",
                "Cannot grant an exemption to a deactivated user.");

        var reason = Enum.Parse<ProximityExemptionReason>(request.Reason);

        var now = DateTime.UtcNow;
        // ValidUntil is an inclusive Sri Lanka business date; the stored instant is
        // the exclusive end, i.e. Colombo midnight the following morning. Going
        // through StartOfDayUtc keeps that 5.5-hour offset correct - a UTC-midnight
        // cutoff would expire the grant mid-afternoon on its final day.
        var validTo = SriLankaTime.StartOfDayUtc(request.ValidUntil.AddDays(1));

        // A live grant is superseded rather than rejected: the admin's intent when
        // re-granting is always "make it this instead", and revoking-then-granting
        // by hand would produce the same two rows with a gap in between.
        var existing = await _repo.GetEffectiveAsync(userId, now, ct);
        if (existing is not null)
        {
            var tracked = await _repo.GetByIdAsync(existing.Id, ct);
            if (tracked is not null)
            {
                tracked.IsActive = false;
                tracked.RevokedAt = now;
                tracked.RevokedByUserId = callerId;
                tracked.UpdatedAt = now;
                tracked.UpdatedBy = callerId;
            }
        }

        var entity = new UserProximityExemption
        {
            UserId = userId,
            ValidFrom = now,
            ValidTo = validTo,
            Reason = reason,
            Notes = string.IsNullOrWhiteSpace(request.Notes) ? null : request.Notes.Trim(),
            GrantedByUserId = callerId,
            IsActive = true,
            CreatedAt = now,
            UpdatedAt = now,
            CreatedBy = callerId,
            UpdatedBy = callerId
        };

        await _repo.AddAsync(entity, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogWarning(
            "Proximity exemption {ExemptionId} granted to user {UserId} by {CallerId}: {Reason} until {ValidTo:o}. Superseded: {SupersededId}",
            entity.Id, userId, callerId, reason, validTo, existing?.Id);

        return MapToDto(entity, user, grantedByName: null, now);
    }

    public async Task<UserProximityExemptionDto> RevokeAsync(
        int exemptionId, RevokeProximityExemptionRequest request, int callerId, CancellationToken ct = default)
    {
        var entity = await _repo.GetByIdAsync(exemptionId, ct)
            ?? throw new NotFoundException("ProximityExemption", exemptionId);

        if (!entity.IsActive)
            throw new BusinessRuleException(
                "PROXIMITY_EXEMPTION_ALREADY_REVOKED",
                "This exemption has already been revoked.");

        _repo.ApplyConcurrencyToken(entity, request.RowVersion);

        var now = DateTime.UtcNow;
        entity.IsActive = false;
        entity.RevokedAt = now;
        entity.RevokedByUserId = callerId;
        entity.UpdatedAt = now;
        entity.UpdatedBy = callerId;

        await _repo.SaveChangesAsync(ct);

        _logger.LogWarning(
            "Proximity exemption {ExemptionId} for user {UserId} revoked by {CallerId}",
            entity.Id, entity.UserId, callerId);

        // Nothing to invalidate: the policy is resolved per request and is never
        // cached alongside the per-route outlet list, so the next bill POST already
        // sees this. The rep's app catches up on its next outlet sync, or when a
        // bill comes back refused with OUTLET_OUT_OF_RANGE.
        return MapToDto(entity, entity.User, entity.GrantedByUser?.Name, now);
    }

    public async Task<IEnumerable<UserProximityExemptionDto>> GetHistoryAsync(
        int userId, CancellationToken ct = default)
    {
        var user = await _repo.GetUserAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        var rows = await _repo.GetHistoryByUserIdAsync(userId, ct);
        var now = DateTime.UtcNow;
        return rows.Select(r => MapToDto(r, user, r.GrantedByUser?.Name, now)).ToList();
    }

    public async Task<UserProximityExemptionDto?> GetCurrentAsync(int userId, CancellationToken ct = default)
    {
        var user = await _repo.GetUserAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        var now = DateTime.UtcNow;
        var current = await _repo.GetEffectiveAsync(userId, now, ct);
        if (current is null) return null;

        var withNames = await _repo.GetByIdAsync(current.Id, ct);
        return MapToDto(withNames ?? current, user, withNames?.GrantedByUser?.Name, now);
    }

    public async Task<UserProximityExemptionListDto> GetActiveAsync(
        int page, int pageSize, string? search = null, CancellationToken ct = default)
    {
        if (page < 1) page = 1;
        if (pageSize is < 1 or > 200) pageSize = 20;

        var now = DateTime.UtcNow;
        var (items, total) = await _repo.GetActiveAsync((page - 1) * pageSize, pageSize, now, search, ct);

        return new UserProximityExemptionListDto(
            items.Select(x => MapToDto(x, x.User, x.GrantedByUser?.Name, now)).ToList(),
            total, page, pageSize);
    }

    private static UserProximityExemptionDto MapToDto(
        UserProximityExemption e, User? user, string? grantedByName, DateTime now)
        => new(
            Id: e.Id,
            UserId: e.UserId,
            Name: user?.Name ?? string.Empty,
            Username: user?.Username ?? string.Empty,
            ValidFrom: e.ValidFrom,
            ValidTo: e.ValidTo,
            // ValidTo is the exclusive Colombo-midnight boundary, so the inclusive
            // business date the admin picked is the one the last tick before it
            // falls on.
            ValidUntilDate: SriLankaTime.BusinessDateOf(e.ValidTo.AddTicks(-1)),
            Reason: e.Reason.ToString(),
            Notes: e.Notes,
            GrantedByUserId: e.GrantedByUserId,
            GrantedByUserName: grantedByName,
            RevokedAt: e.RevokedAt,
            RevokedByUserId: e.RevokedByUserId,
            IsActive: e.IsActive,
            IsCurrentlyEffective: e.IsActive && e.ValidFrom <= now && e.ValidTo > now,
            RowVersion: e.RowVersion,
            CreatedAt: e.CreatedAt,
            UpdatedAt: e.UpdatedAt);
}
