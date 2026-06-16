using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using sfa_api.Common.Errors;
using sfa_api.Features.Notifications.Services;

namespace sfa_api.Features.Notifications.Controllers;

[ApiController]
[Route("api/v1/notifications")]
[Authorize]
public class NotificationsController(INotificationHistoryService notificationHistoryService) : ControllerBase
{
    private readonly INotificationHistoryService _service = notificationHistoryService;

    private int CallerId =>
        int.TryParse(User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : throw new AuthenticationException("AUTH_INVALID_TOKEN", "Invalid token.");

    /// <summary>
    /// GET /api/v1/notifications
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetNotifications(
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken ct = default)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetPagedAsync(CallerId, page, pageSize, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// GET /api/v1/notifications/unread-count
    /// </summary>
    [HttpGet("unread-count")]
    public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        var result = await _service.GetUnreadCountAsync(CallerId, ct);
        return Ok(ResponseHelper.Ok(result, correlationId));
    }

    /// <summary>
    /// POST /api/v1/notifications/{id}/read
    /// </summary>
    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkRead(int id, CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        await _service.MarkReadAsync(id, CallerId, ct);
        return Ok(ResponseHelper.Ok(true, correlationId));
    }

    /// <summary>
    /// POST /api/v1/notifications/read-all
    /// </summary>
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllRead(CancellationToken ct)
    {
        var correlationId = HttpContext.Items["CorrelationId"]?.ToString() ?? string.Empty;
        await _service.MarkAllReadAsync(CallerId, ct);
        return Ok(ResponseHelper.Ok(true, correlationId));
    }
}
