using System.Threading.Channels;

namespace sfa_api.Infrastructure.Notifications;

/// <summary>One FCM push to one device token.</summary>
public sealed record PushMessage(
    int UserId,
    string Token,
    string Title,
    string Body,
    IReadOnlyDictionary<string, string>? Data);

/// <summary>
/// In-process hand-off between request code and <see cref="PushNotificationDispatcher"/>, so a
/// request never waits on Firebase. Pushes are best-effort: the notification inbox row is persisted
/// in-request before enqueueing, so a push lost to a full queue or a process restart still shows up
/// in the user's inbox.
/// </summary>
public interface IPushNotificationQueue
{
    /// <summary>Enqueues without blocking. Returns false (and the push is dropped) when the queue is full.</summary>
    bool TryEnqueue(PushMessage message);

    ChannelReader<PushMessage> Reader { get; }
}

public sealed class PushNotificationQueue : IPushNotificationQueue
{
    /// <summary>
    /// Far above any realistic burst (500 reps); bounded so a Firebase outage can't grow memory
    /// without limit.
    /// </summary>
    public const int Capacity = 10_000;

    private readonly Channel<PushMessage> _channel = Channel.CreateBounded<PushMessage>(
        new BoundedChannelOptions(Capacity)
        {
            FullMode     = BoundedChannelFullMode.DropWrite,
            SingleReader = true,
            SingleWriter = false,
        });

    public bool TryEnqueue(PushMessage message) => _channel.Writer.TryWrite(message);

    public ChannelReader<PushMessage> Reader => _channel.Reader;
}
