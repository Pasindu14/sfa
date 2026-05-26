namespace sfa_api.Features.Notifications.DTOs;

public record NotificationDto(int Id, string Title, string Body, string? Data, bool IsRead, DateTime CreatedAt);
public record NotificationListDto(IEnumerable<NotificationDto> Notifications, int TotalCount, int Page, int PageSize);
public record UnreadCountDto(int Count);
