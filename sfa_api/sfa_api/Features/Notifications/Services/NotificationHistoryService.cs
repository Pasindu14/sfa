using sfa_api.Common.Errors;
using sfa_api.Common.Extensions;
using sfa_api.Features.Notifications.DTOs;
using sfa_api.Features.Notifications.Repositories;

namespace sfa_api.Features.Notifications.Services;

public class NotificationHistoryService(INotificationRepository repository) : INotificationHistoryService
{
    private readonly INotificationRepository _repository = repository;

    public async Task<NotificationListDto> GetPagedAsync(int userId, int page, int pageSize, CancellationToken ct = default)
    {
        var (normPage, normPageSize, skip) = PaginationHelper.Normalize(page, pageSize);
        var (items, totalCount) = await _repository.GetPagedAsync(userId, skip, normPageSize, ct);
        var dtos = items.Select(n => new NotificationDto(n.Id, n.Title, n.Body, n.Data, n.IsRead, n.CreatedAt));
        return new NotificationListDto(dtos, totalCount, normPage, normPageSize);
    }

    public async Task<UnreadCountDto> GetUnreadCountAsync(int userId, CancellationToken ct = default)
    {
        var count = await _repository.GetUnreadCountAsync(userId, ct);
        return new UnreadCountDto(count);
    }

    public async Task MarkReadAsync(int id, int callerId, CancellationToken ct = default)
    {
        var affected = await _repository.MarkReadAsync(id, callerId, ct);
        if (affected == 0)
            throw new NotFoundException("Notification", id);
    }

    public Task MarkAllReadAsync(int callerId, CancellationToken ct = default) =>
        _repository.MarkAllReadAsync(callerId, ct);
}
