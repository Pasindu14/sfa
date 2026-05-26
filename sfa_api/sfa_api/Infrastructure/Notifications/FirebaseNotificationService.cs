using System.Text.Json;
using FirebaseAdmin.Messaging;
using sfa_api.Features.Notifications.Repositories;
using sfa_api.Features.Users.Repositories;
using FcmNotification = FirebaseAdmin.Messaging.Notification;
using NotificationEntity = sfa_api.Features.Notifications.Entities.Notification;

namespace sfa_api.Infrastructure.Notifications;

public class FirebaseNotificationService(
    IUserRepository userRepository,
    INotificationRepository notificationRepository,
    ILogger<FirebaseNotificationService> logger) : INotificationService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly INotificationRepository _notificationRepository = notificationRepository;
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
                await SendToTokenAsync(token, userId, title, body, data);
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
                await SendToTokenAsync(token, userId, title, body, data);
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
                await SendToTokenAsync(token, userId, title, body, data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Notification failed for sales reps of distributor {DistributorId}", distributorId);
        }
    }

    private async Task SendToTokenAsync(string token, int userId, string title, string body, Dictionary<string, string>? data)
    {
        try
        {
            var message = new Message
            {
                Token = token,
                Notification = new FcmNotification { Title = title, Body = body },
                Data = data ?? [],
                Android = new AndroidConfig { Priority = Priority.High },
                Apns = new ApnsConfig
                {
                    Aps = new Aps { Sound = "default" }
                }
            };
            await FirebaseMessaging.DefaultInstance.SendAsync(message);
        }
        catch (FirebaseMessagingException ex) when (ex.MessagingErrorCode == MessagingErrorCode.Unregistered)
        {
            // Token is stale (app uninstalled / token rotated) — clean it up silently
            _logger.LogInformation("Stale FCM token cleared for user {UserId}", userId);
            await _userRepository.ClearFcmTokenAsync(userId, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FCM send failed for user {UserId}", userId);
        }
    }
}
