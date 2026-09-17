using sfa_api.Features.Users.Entities;

namespace sfa_api.Features.Users.DTOs;

/// <summary>
/// Read-only slice of a <see cref="User"/> for permission / scoping checks (e.g. "which distributor
/// is this caller linked to?"), loaded with a no-tracking column projection. Use
/// <c>IUserRepository.GetUserByIdAsync</c> instead when the entity must be mutated and saved or
/// navigation properties are needed. Soft-deleted users are excluded (User's global query filter);
/// deactivated users are not — the same visibility as <c>GetUserByIdAsync</c>.
/// </summary>
public sealed record UserAccessInfo(
    int Id,
    string Name,
    UserRole Role,
    int? DistributorId,
    bool IsActive);
