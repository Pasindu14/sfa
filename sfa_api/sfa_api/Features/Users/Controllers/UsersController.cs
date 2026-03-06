using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Users.Requests;
using sfa_api.Features.Users.Services;

namespace sfa_api.Features.Users.Controllers;

[ApiController]
[Route("api/v1/users")]
public class UsersController(
    IUserService userService,
    IValidator<CreateUserRequest> createUserValidator,
    IValidator<UpdateUserRequest> updateUserValidator,
    IValidator<ChangePasswordRequest> changePasswordValidator,
    IValidator<ResetPasswordRequest> resetPasswordValidator) : ControllerBase
{
    private readonly IUserService _userService = userService;
    private readonly IValidator<CreateUserRequest> _createUserValidator = createUserValidator;
    private readonly IValidator<UpdateUserRequest> _updateUserValidator = updateUserValidator;
    private readonly IValidator<ChangePasswordRequest> _changePasswordValidator = changePasswordValidator;
    private readonly IValidator<ResetPasswordRequest> _resetPasswordValidator = resetPasswordValidator;

    /// <summary>
    /// GET /api/v1/users/{id}
    /// </summary>
    [HttpGet("{id}")]
    [Authorize]
    public async Task<IActionResult> GetUserById(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _userService.GetUserByIdAsync(id, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/users
    /// </summary>
    [HttpGet]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GetAllUsers(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 10,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _userService.GetAllUsersAsync(page, pageSize, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/users
    /// </summary>
    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        var validation = await _createUserValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var fields = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Common.Errors.ValidationException(fields);
        }

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _userService.CreateUserAsync(request, callerId, ct);
        return CreatedAtAction(nameof(GetUserById), new { id = result.Id },
            ResponseHelper.Created(result, correlationId));
    }

    /// <summary>
    /// PUT /api/v1/users/{id}
    /// Admin can update any user; a user can update their own profile.
    /// </summary>
    [HttpPut("{id}")]
    [Authorize]
    public async Task<IActionResult> UpdateUser(int id, [FromBody] UpdateUserRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var currentUserId))
            throw new AuthenticationException("AUTH_INVALID_TOKEN", "Invalid token.");

        var roleClaim = User.FindFirstValue(ClaimTypes.Role);

        if (roleClaim != "Admin" && currentUserId != id)
            throw new AuthorizationException("this user");

        var validation = await _updateUserValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var fields = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Common.Errors.ValidationException(fields);
        }

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        var result = await _userService.UpdateUserAsync(id, request, callerId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// DELETE /api/v1/users/{id}
    /// </summary>
    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteUser(int id, CancellationToken ct)
    {
        await _userService.DeleteUserAsync(id, ct);
        return NoContent();
    }

    /// <summary>
    /// POST /api/v1/users/{id}/change-password
    /// Only the owner can change their own password.
    /// </summary>
    [HttpPost("{id}/change-password")]
    [Authorize]
    public async Task<IActionResult> ChangePassword(int id, [FromBody] ChangePasswordRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var currentUserIdForPassword))
            throw new AuthenticationException("AUTH_INVALID_TOKEN", "Invalid token.");

        if (currentUserIdForPassword != id)
            throw new AuthorizationException("other users");

        var validation = await _changePasswordValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var fields = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Common.Errors.ValidationException(fields);
        }

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        await _userService.ChangePasswordAsync(id, request, callerId, ct);
        return Ok(ResponseHelper.Ok("Password changed successfully.", correlationId));
    }

    /// <summary>
    /// POST /api/v1/users/{id}/reset-password
    /// Admin only.
    /// </summary>
    [HttpPost("{id}/reset-password")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ResetPassword(int id, [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        var validation = await _resetPasswordValidator.ValidateAsync(request, ct);
        if (!validation.IsValid)
        {
            var fields = validation.Errors
                .GroupBy(e => e.PropertyName)
                .ToDictionary(g => g.Key, g => g.Select(e => e.ErrorMessage).ToArray());
            throw new Common.Errors.ValidationException(fields);
        }

        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        await _userService.ResetPasswordAsync(id, request, callerId, ct);
        return Ok(ResponseHelper.Ok("Password reset successfully.", correlationId));
    }

    /// <summary>
    /// POST /api/v1/users/{id}/deactivate
    /// Admin only — sets IsActive = false without deleting the record.
    /// </summary>
    [HttpPost("{id}/deactivate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeactivateUser(int id, CancellationToken ct)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        await _userService.DeactivateUserAsync(id, callerId, ct);
        return NoContent();
    }

    /// <summary>
    /// POST /api/v1/users/{id}/activate
    /// Admin only — sets IsActive = true.
    /// </summary>
    [HttpPost("{id}/activate")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ActivateUser(int id, CancellationToken ct)
    {
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var callerId);
        await _userService.ActivateUserAsync(id, callerId, ct);
        return NoContent();
    }
}
