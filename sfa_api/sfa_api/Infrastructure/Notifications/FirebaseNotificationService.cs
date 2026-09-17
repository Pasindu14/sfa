using System.Text.Json;
using sfa_api.Features.Notifications.Repositories;
using sfa_api.Features.Users.Repositories;
using NotificationEntity = sfa_api.Features.Notifications.Entities.Notification;

namespace sfa_api.Infrastructure.Notifications;

/// <summary>
/// Persists the in-app inbox row(s) in-request, then hands the FCM pushes to
/// <see cref="IPushNotificationQueue"/>; <see cref="PushNotificationDispatcher"/> sends them in the
/// background. The request never waits on Firebase, and no failure here is ever thrown to the caller.
/// </summary>
public class FirebaseNotificationService(
    IUserRepository userRepository,
    INotificationRepository notificationRepository,
    IPushNotificationQueue pushQueue,
    ILogger<FirebaseNotificationService> logger) : INotificationService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly INotificationRepository _notificationRepository = notificationRepository;
    private readonly IPushNotificationQueue _pushQueue = pushQueue;
    private readonly ILogger<FirebaseNotificationService> _logger = logger;

    public async Task SendToUserAsync(int userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        try
        {
            // Persist first — inbox is complete even if FCM fails
            await _notificationRepository.CreateAsync(new NotificationEntity
            {
                UserId = userId,
                Title = title,
                Body = body,
                Data = data is { Count: > 0 } ? JsonSerializer.Serialize(data) : null,
            }, ct);

            var token = await _userRepository.GetFcmTokenByUserIdAsync(userId, ct);
            if (!string.IsNullOrWhiteSpace(token))
                EnqueuePush(userId, token, title, body, data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Notification failed for user {UserId}", userId);
        }
    }

    public async Task SendToDistributorUsersAsync(int distributorId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        try
        {
            var users = await _userRepository.GetFcmTokensByDistributorIdAsync(distributorId, ct);
            var dataJson = data is { Count: > 0 } ? JsonSerializer.Serialize(data) : null;

            await _notificationRepository.CreateManyAsync(
                users.Select(u => new NotificationEntity { UserId = u.UserId, Title = title, Body = body, Data = dataJson }), ct);

            foreach (var (userId, token) in users)
                EnqueuePush(userId, token, title, body, data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Notification failed for distributor {DistributorId}", distributorId);
        }
    }

    public async Task SendToDistributorSalesRepsAsync(int distributorId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        try
        {
            var users = await _userRepository.GetFcmTokensByDistributorSalesRepsAsync(distributorId, ct);
            var dataJson = data is { Count: > 0 } ? JsonSerializer.Serialize(data) : null;

            await _notificationRepository.CreateManyAsync(
                users.Select(u => new NotificationEntity { UserId = u.UserId, Title = title, Body = body, Data = dataJson }), ct);

            foreach (var (userId, token) in users)
                EnqueuePush(userId, token, title, body, data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Notification failed for sales reps of distributor {DistributorId}", distributorId);
        }
    }

    private void EnqueuePush(int userId, string token, string title, string body, Dictionary<string, string>? data)
    {
        if (string.IsNullOrWhiteSpace(token)) return;

        // Copy the payload: callers reuse one dictionary across several recipients.
        var payload = data is { Count: > 0 } ? new Dictionary<string, string>(data) : null;
        if (!_pushQueue.TryEnqueue(new PushMessage(userId, token, title, body, payload)))
            _logger.LogWarning("Push notification queue full; dropped push for user {UserId}", userId);
    }
}
