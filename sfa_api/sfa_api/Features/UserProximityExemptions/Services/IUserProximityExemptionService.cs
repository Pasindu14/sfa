using sfa_api.Features.UserProximityExemptions.DTOs;
using sfa_api.Features.UserProximityExemptions.Requests;

namespace sfa_api.Features.UserProximityExemptions.Services;

public interface IUserProximityExemptionService
{
    Task<UserProximityExemptionDto> GrantAsync(
        int userId, GrantProximityExemptionRequest request, int callerId, CancellationToken ct = default);

    Task<UserProximityExemptionDto> RevokeAsync(
        int exemptionId, RevokeProximityExemptionRequest request, int callerId, CancellationToken ct = default);

    /// <summary>Grant history for one rep, newest first.</summary>
    Task<IEnumerable<UserProximityExemptionDto>> GetHistoryAsync(int userId, CancellationToken ct = default);

    /// <summary>The grant in force for one rep right now, or null.</summary>
    Task<UserProximityExemptionDto?> GetCurrentAsync(int userId, CancellationToken ct = default);

    /// <summary>Every rep currently exempt — the review list.</summary>
    Task<UserProximityExemptionListDto> GetActiveAsync(
        int page, int pageSize, string? search = null, CancellationToken ct = default);
}
