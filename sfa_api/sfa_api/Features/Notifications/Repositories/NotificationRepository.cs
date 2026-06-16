using Microsoft.EntityFrameworkCore;
using sfa_api.Features.Notifications.Entities;
using sfa_api.Infrastructure.Persistence;

namespace sfa_api.Features.Notifications.Repositories;

public class NotificationRepository(AppDbContext context) : INotificationRepository
{
    private readonly AppDbContext _context = context;

    public async Task CreateAsync(Notification notification, CancellationToken ct = default)
    {
        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync(ct);
    }

    public async Task CreateManyAsync(IEnumerable<Notification> notifications, CancellationToken ct = default)
    {
        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync(ct);
    }

    public async Task<(IEnumerable<Notification> Items, int TotalCount)> GetPagedAsync(int userId, int skip, int take, CancellationToken ct = default)
    {
        var query = _context.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var totalCount = await query.CountAsync(ct);
        var items = await query.Skip(skip).Take(take).ToListAsync(ct);
        return (items, totalCount);
    }

    public Task<int> GetUnreadCountAsync(int userId, CancellationToken ct = default) =>
        _context.Notifications
            .AsNoTracking()
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);

    public Task<int> MarkReadAsync(int id, int userId, CancellationToken ct = default) =>
        _context.Notifications
            .Where(n => n.Id == id && n.UserId == userId)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);

    public async Task MarkAllReadAsync(int userId, CancellationToken ct = default)
    {
        await _context.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(s => s.SetProperty(n => n.IsRead, true), ct);
    }

    public Task SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);
}
