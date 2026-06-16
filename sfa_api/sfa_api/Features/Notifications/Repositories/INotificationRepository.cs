using sfa_api.Features.Notifications.Entities;

namespace sfa_api.Features.Notifications.Repositories;

public interface INotificationRepository
{
    Task CreateAsync(Notification notification, CancellationToken ct = default);
    Task CreateManyAsync(IEnumerable<Notification> notifications, CancellationToken ct = default);
    Task<(IEnumerable<Notification> Items, int TotalCount)> GetPagedAsync(int userId, int skip, int take, CancellationToken ct = default);
    Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default);
    Task<int> MarkReadAsync(int id, int userId, CancellationToken ct = default);
    Task MarkAllReadAsync(int userId, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
