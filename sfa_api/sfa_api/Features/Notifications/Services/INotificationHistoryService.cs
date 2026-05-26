using sfa_api.Features.Notifications.DTOs;

namespace sfa_api.Features.Notifications.Services;

public interface INotificationHistoryService
{
    Task<NotificationListDto> GetPagedAsync(int userId, int page, int pageSize, CancellationToken ct = default);
    Task<UnreadCountDto> GetUnreadCountAsync(int userId, CancellationToken ct = default);
    Task MarkReadAsync(int id, int callerId, CancellationToken ct = default);
    Task MarkAllReadAsync(int callerId, CancellationToken ct = default);
}
