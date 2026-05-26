using FirebaseAdmin.Messaging;
using sfa_api.Features.Users.Repositories;

namespace sfa_api.Infrastructure.Notifications;

public class FirebaseNotificationService(
    IUserRepository userRepository,
    ILogger<FirebaseNotificationService> logger) : INotificationService
{
    private readonly IUserRepository _userRepository = userRepository;
    private readonly ILogger<FirebaseNotificationService> _logger = logger;

    public async Task SendToUserAsync(int userId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        try
        {
            var token = await _userRepository.GetFcmTokenByUserIdAsync(userId, ct);
            if (string.IsNullOrWhiteSpace(token)) return;
            await SendToTokenAsync(token, userId, title, body, data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FCM notification failed for user {UserId}", userId);
        }
    }

    public async Task SendToDistributorUsersAsync(int distributorId, string title, string body, Dictionary<string, string>? data = null, CancellationToken ct = default)
    {
        try
        {
            var users = await _userRepository.GetFcmTokensByDistributorIdAsync(distributorId, ct);
            foreach (var (userId, token) in users)
                await SendToTokenAsync(token, userId, title, body, data);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "FCM notification failed for distributor {DistributorId}", distributorId);
        }
    }

    private async Task SendToTokenAsync(string token, int userId, string title, string body, Dictionary<string, string>? data)
    {
        try
        {
            var message = new Message
            {
                Token = token,
                Notification = new Notification { Title = title, Body = body },
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
