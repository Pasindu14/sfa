using sfa_api.Common.Errors;
using sfa_api.Features.Distributors.Repositories;
using sfa_api.Features.Users.DTOs;
using sfa_api.Features.Users.Entities;
using sfa_api.Features.Users.Repositories;
using sfa_api.Features.Users.Requests;

namespace sfa_api.Features.Users.Services;

public class UserService(
    IUserRepository repo,
    IDistributorRepository distributorRepo,
    ILogger<UserService> logger) : IUserService
{
    private readonly IUserRepository _repo = repo;
    private readonly IDistributorRepository _distributorRepo = distributorRepo;
    private readonly ILogger<UserService> _logger = logger;

    public async Task<UserDto> GetUserByIdAsync(int userId, CancellationToken ct = default)
    {
        var user = await _repo.GetUserByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);
        return MapToDto(user);
    }

    public async Task<UserListDto> GetAllUsersAsync(int page, int pageSize, string? search = null, string? role = null, CancellationToken ct = default)
    {
        var skip = (page - 1) * pageSize;
        var (users, totalCount) = await _repo.GetAllUsersAsync(skip, pageSize, search, role, ct);
        return new UserListDto(
            Users: users.Select(MapToDto),
            TotalCount: totalCount,
            Page: page,
            PageSize: pageSize
        );
    }

    public async Task<UserDto> CreateUserAsync(CreateUserRequest request, int? callerId, CancellationToken ct = default)
    {
        if (await _repo.ExistsByUsernameAsync(request.Username, ct))
            throw new DuplicateResourceException("Username");

        if (await _repo.ExistsByEmailAsync(request.Email, ct))
            throw new DuplicateResourceException("Email");

        if (await _repo.ExistsByPhoneAsync(request.Phone, ct))
            throw new DuplicateResourceException("Phone");

        if (!Enum.TryParse<UserRole>(request.Role, out var role))
            throw new ValidationException(new Dictionary<string, string[]>
                { { "Role", new[] { "Invalid role." } } });

        int? distributorId = null;
        if (role == UserRole.Distributor)
        {
            var distributor = await _distributorRepo.GetByIdAsync(request.DistributorId!.Value, ct)
                ?? throw new NotFoundException("Distributor", request.DistributorId.Value);
            distributorId = distributor.Id;
        }

        var user = new User
        {
            Name = request.Name,
            Username = request.Username.ToLowerInvariant(),
            Email = request.Email.ToLowerInvariant(),
            Phone = request.Phone,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = role,
            DistributorId = distributorId,
            DeviceId = request.DeviceId,
            IsActive = true,
            CreatedBy = callerId,
            UpdatedBy = callerId,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _repo.CreateUserAsync(user, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} created", user.Id);
        return MapToDto(user);
    }

    public async Task<UserDto> UpdateUserAsync(int userId, UpdateUserRequest request, int? callerId, CancellationToken ct = default)
    {
        var user = await _repo.GetUserByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        if (await _repo.ExistsByUsernameAsync(request.Username, userId, ct))
            throw new DuplicateResourceException("Username");

        if (await _repo.ExistsByEmailAsync(request.Email, userId, ct))
            throw new DuplicateResourceException("Email");

        if (await _repo.ExistsByPhoneAsync(request.Phone, userId, ct))
            throw new DuplicateResourceException("Phone");

        if (!Enum.TryParse<UserRole>(request.Role, out var role))
            throw new ValidationException(new Dictionary<string, string[]>
                { { "Role", new[] { "Invalid role." } } });

        if (role == UserRole.Distributor)
        {
            var distributor = await _distributorRepo.GetByIdAsync(request.DistributorId!.Value, ct)
                ?? throw new NotFoundException("Distributor", request.DistributorId.Value);
            user.DistributorId = distributor.Id;
        }
        else
        {
            user.DistributorId = null;
        }

        user.Name = request.Name;
        user.Username = request.Username.ToLowerInvariant();
        user.Email = request.Email.ToLowerInvariant();
        user.Phone = request.Phone;
        user.Role = role;
        user.DeviceId = request.DeviceId;
        user.UpdatedBy = callerId;
        user.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateUserAsync(user, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} updated", userId);
        return MapToDto(user);
    }

    public async Task DeleteUserAsync(int userId, CancellationToken ct = default)
    {
        _ = await _repo.GetUserByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        await _repo.DeleteUserAsync(userId, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} deleted", userId);
    }

    public async Task ChangePasswordAsync(int userId, ChangePasswordRequest request, int? callerId, CancellationToken ct = default)
    {
        var user = await _repo.GetUserByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        if (!BCrypt.Net.BCrypt.Verify(request.CurrentPassword, user.PasswordHash))
            throw new ValidationException(new Dictionary<string, string[]>
                { { "CurrentPassword", new[] { "Current password is incorrect." } } });

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedBy = callerId;
        user.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateUserAsync(user, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Password changed for user {UserId}", userId);
    }

    public async Task ResetPasswordAsync(int userId, ResetPasswordRequest request, int? callerId, CancellationToken ct = default)
    {
        var user = await _repo.GetUserByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.UpdatedBy = callerId;
        user.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateUserAsync(user, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Password reset for user {UserId}", userId);
    }

    public async Task DeactivateUserAsync(int userId, int? callerId, CancellationToken ct = default)
    {
        var user = await _repo.GetUserByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        user.IsActive = false;
        user.UpdatedBy = callerId;
        user.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateUserAsync(user, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} deactivated", userId);
    }

    public async Task ActivateUserAsync(int userId, int? callerId, CancellationToken ct = default)
    {
        var user = await _repo.GetUserByIdAsync(userId, ct)
            ?? throw new NotFoundException("User", userId);

        user.IsActive = true;
        user.UpdatedBy = callerId;
        user.UpdatedAt = DateTime.UtcNow;

        await _repo.UpdateUserAsync(user, ct);
        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("User {UserId} activated", userId);
    }

    public Task UpdateFcmTokenAsync(int userId, string token, CancellationToken ct = default)
        => _repo.UpdateFcmTokenAsync(userId, token, ct);

    public Task ClearFcmTokenAsync(int userId, CancellationToken ct = default)
        => _repo.ClearFcmTokenAsync(userId, ct);

    private static UserDto MapToDto(User user) => new(
        Id: user.Id,
        Name: user.Name,
        Username: user.Username,
        Email: user.Email,
        Phone: user.Phone,
        Role: user.Role.ToString(),
        DistributorId: user.DistributorId,
        DistributorName: user.Distributor?.Name,
        IsActive: user.IsActive,
        CreatedAt: user.CreatedAt,
        UpdatedAt: user.UpdatedAt
    );
}
