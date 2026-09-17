using FirebaseAdmin;
using FirebaseAdmin.Messaging;
using sfa_api.Features.Users.Repositories;
using FcmNotification = FirebaseAdmin.Messaging.Notification;

namespace sfa_api.Infrastructure.Notifications;

/// <summary>
/// Drains <see cref="IPushNotificationQueue"/> and sends via Firebase in batches
/// (<see cref="FirebaseMessaging.SendEachAsync(IEnumerable{Message})"/>, up to
/// <see cref="MaxBatchSize"/> messages per call). Every failure is logged here — nothing reaches a
/// request. Tokens Firebase reports as unregistered are cleared, as before.
/// </summary>
public sealed class PushNotificationDispatcher(
    IPushNotificationQueue queue,
    IServiceScopeFactory scopeFactory,
    ILogger<PushNotificationDispatcher> logger) : BackgroundService
{
    /// <summary>FCM's per-call limit for SendEach.</summary>
    public const int MaxBatchSize = 500;

    private static readonly TimeSpan ShutdownDrainTimeout = TimeSpan.FromSeconds(5);

    private readonly IPushNotificationQueue _queue = queue;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly ILogger<PushNotificationDispatcher> _logger = logger;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        try
        {
            while (await _queue.Reader.WaitToReadAsync(stoppingToken))
                await SendAvailableAsync(stoppingToken);
        }
        catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
        {
            // Shutting down — best-effort flush of whatever is already queued.
            using var drainCts = new CancellationTokenSource(ShutdownDrainTimeout);
            try
            {
                await SendAvailableAsync(drainCts.Token);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Push notification queue not fully drained at shutdown");
            }
        }
    }

    private async Task SendAvailableAsync(CancellationToken ct)
    {
        var batch = new List<PushMessage>(MaxBatchSize);
        while (true)
        {
            batch.Clear();
            while (batch.Count < MaxBatchSize && _queue.Reader.TryRead(out var message))
                batch.Add(message);

            if (batch.Count == 0) return;

            try
            {
                await SendBatchAsync(batch, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "FCM batch send failed for {Count} message(s)", batch.Count);
            }
        }
    }

    private async Task SendBatchAsync(IReadOnlyList<PushMessage> batch, CancellationToken ct)
    {
        if (FirebaseApp.DefaultInstance is null)
        {
            // FIREBASE_SERVICE_ACCOUNT_JSON not configured (dev/test) — inbox rows are already saved.
            _logger.LogDebug("Firebase not configured; dropping {Count} push notification(s)", batch.Count);
            return;
        }

        var messages = batch.Select(m => new Message
        {
            Token        = m.Token,
            Notification = new FcmNotification { Title = m.Title, Body = m.Body },
            Data         = m.Data ?? new Dictionary<string, string>(),
            Android      = new AndroidConfig { Priority = Priority.High },
            Apns         = new ApnsConfig { Aps = new Aps { Sound = "default" } }
        }).ToList();

        var response = await FirebaseMessaging.DefaultInstance.SendEachAsync(messages, ct);
        if (response.FailureCount == 0) return;

        var staleUserIds = new HashSet<int>();
        for (var i = 0; i < response.Responses.Count; i++)
        {
            var result = response.Responses[i];
            if (result.IsSuccess) continue;

            if (result.Exception?.MessagingErrorCode == MessagingErrorCode.Unregistered)
                staleUserIds.Add(batch[i].UserId);
            else
                _logger.LogWarning(result.Exception, "FCM send failed for user {UserId}", batch[i].UserId);
        }

        if (staleUserIds.Count == 0) return;

        // Token is stale (app uninstalled / token rotated) — clean it up silently.
        await using var scope = _scopeFactory.CreateAsyncScope();
        var users = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        foreach (var userId in staleUserIds)
        {
            try
            {
                await users.ClearFcmTokenAsync(userId, CancellationToken.None);
                _logger.LogInformation("Stale FCM token cleared for user {UserId}", userId);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to clear stale FCM token for user {UserId}", userId);
            }
        }
    }
}
