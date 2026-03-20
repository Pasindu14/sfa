using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Auth.Repositories;
using sfa_api.Features.Auth.Requests;
using sfa_api.Features.Auth.Services;

namespace sfa_api.Features.Auth.Controllers;

[ApiController]
[Route("api/v1/auth")]
public class AuthController(
    IAuthService authService,
    IValidator<LoginRequest> loginValidator,
    IValidator<RefreshRequest> refreshValidator,
    IAuthRepository authRepository,
    IJwtTokenHelper jwtTokenHelper,
    IWebHostEnvironment env) : ControllerBase
{
    private readonly IAuthService _authService = authService;
    private readonly IValidator<LoginRequest> _loginValidator = loginValidator;
    private readonly IValidator<RefreshRequest> _refreshValidator = refreshValidator;
    private readonly IAuthRepository _authRepository = authRepository;
    private readonly IJwtTokenHelper _jwtTokenHelper = jwtTokenHelper;
    private readonly IWebHostEnvironment _env = env;

    /// <summary>
    /// POST /api/v1/auth/login
    /// </summary>
    [HttpPost("login")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _loginValidator.ValidateOrThrowAsync(request, ct);

        var result = await _authService.LoginAsync(request, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/auth/refresh
    /// </summary>
    [HttpPost("refresh")]
    [AllowAnonymous]
    [EnableRateLimiting("auth")]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        await _refreshValidator.ValidateOrThrowAsync(request, ct);

        var result = await _authService.RefreshAsync(request, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/auth/logout
    /// </summary>
    [HttpPost("logout")]
    [AllowAnonymous]
    public async Task<IActionResult> Logout(
        [FromBody] LogoutRequest request, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        if (string.IsNullOrEmpty(request.RefreshToken))
            return Ok(ResponseHelper.Ok("Logged out.", correlationId));

        await _authService.LogoutAsync(request.RefreshToken, ct);
        return Ok(ResponseHelper.Ok("Logged out successfully.", correlationId));
    }

    /// <summary>
    /// POST /api/v1/auth/logout-all
    /// </summary>
    [HttpPost("logout-all")]
    [Authorize]
    public async Task<IActionResult> LogoutAll(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (!int.TryParse(userIdClaim, out var userId))
            throw new AuthenticationException("AUTH_INVALID_TOKEN", "Invalid token.");

        await _authService.LogoutAllAsync(userId, ct);
        return Ok(ResponseHelper.Ok("Logged out from all devices successfully.", correlationId));
    }

    /// <summary>
    /// POST /api/v1/auth/dev-token
    /// Generates a long-lived test token. Only available in Development environment.
    /// </summary>
    [HttpPost("dev-token")]
    [AllowAnonymous]
    public async Task<IActionResult> GenerateDevToken(
        [FromBody] DevTokenRequest request, CancellationToken ct)
    {
        if (!_env.IsDevelopment())
            throw new AuthorizationException("dev-token endpoint");

        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;

        var user = await _authRepository.GetUserByIdAsync(request.UserId, ct)
            ?? throw new NotFoundException("User", request.UserId);

        var expiryDays = request.ExpiryDays is > 0 and <= 3650
            ? request.ExpiryDays.Value
            : 365;

        var token = _jwtTokenHelper.GenerateTestToken(user, expiryDays);

        return Ok(ResponseHelper.Ok(new
        {
            accessToken = token,
            expiresAt = DateTime.UtcNow.AddDays(expiryDays),
            userId = user.Id,
            userName = user.Name,
            role = user.Role.ToString()
        }, correlationId));
    }
}
