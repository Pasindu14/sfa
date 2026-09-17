using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using sfa_api.Features.Notifications.Repositories;
using sfa_api.Features.Users.Repositories;
using sfa_api.Infrastructure.Notifications;
using NotificationEntity = sfa_api.Features.Notifications.Entities.Notification;

namespace sfa_api.UnitTests.Infrastructure.Notifications;

public class FirebaseNotificationServiceTests
{
    private readonly Mock<IUserRepository> _users = new();
    private readonly Mock<INotificationRepository> _inbox = new();
    private readonly PushNotificationQueue _queue = new();
    private readonly FirebaseNotificationService _sut;

    public FirebaseNotificationServiceTests()
    {
        _sut = new FirebaseNotificationService(_users.Object, _inbox.Object, _queue,
            NullLogger<FirebaseNotificationService>.Instance);
    }

    private List<PushMessage> Drain()
    {
        var items = new List<PushMessage>();
        while (_queue.Reader.TryRead(out var m)) items.Add(m);
        return items;
    }

    [Fact]
    public async Task SendToUser_PersistsInboxRowAndEnqueuesPush()
    {
        _users.Setup(u => u.GetFcmTokenByUserIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync("tok-7");

        await _sut.SendToUserAsync(7, "Title", "Body", new Dictionary<string, string> { ["type"] = "X" });

        _inbox.Verify(i => i.CreateAsync(It.Is<NotificationEntity>(n => n.UserId == 7), It.IsAny<CancellationToken>()), Times.Once);
        var pushes = Drain();
        pushes.Should().ContainSingle();
        pushes[0].UserId.Should().Be(7);
        pushes[0].Token.Should().Be("tok-7");
        pushes[0].Title.Should().Be("Title");
        pushes[0].Body.Should().Be("Body");
        pushes[0].Data!["type"].Should().Be("X");
    }

    [Fact]
    public async Task SendToUser_NoToken_PersistsInboxButEnqueuesNothing()
    {
        _users.Setup(u => u.GetFcmTokenByUserIdAsync(7, It.IsAny<CancellationToken>())).ReturnsAsync((string?)null);

        await _sut.SendToUserAsync(7, "Title", "Body");

        _inbox.Verify(i => i.CreateAsync(It.IsAny<NotificationEntity>(), It.IsAny<CancellationToken>()), Times.Once);
        Drain().Should().BeEmpty();
    }

    [Fact]
    public async Task SendToDistributorUsers_EnqueuesOnePushPerToken()
    {
        _users.Setup(u => u.GetFcmTokensByDistributorIdAsync(3, It.IsAny<CancellationToken>()))
              .ReturnsAsync(new List<(int UserId, string Token)> { (1, "a"), (2, "b") });

        await _sut.SendToDistributorUsersAsync(3, "T", "B");

        Drain().Select(m => (m.UserId, m.Token)).Should().Equal((1, "a"), (2, "b"));
    }

    [Fact]
    public async Task RepositoryFailure_IsSwallowed()
    {
        _inbox.Setup(i => i.CreateAsync(It.IsAny<NotificationEntity>(), It.IsAny<CancellationToken>()))
              .ThrowsAsync(new InvalidOperationException("db down"));

        var act = () => _sut.SendToUserAsync(7, "T", "B");

        await act.Should().NotThrowAsync();
        Drain().Should().BeEmpty();
    }

    [Fact]
    public async Task SharedPayloadDictionary_IsCopiedPerPush()
    {
        _users.Setup(u => u.GetFcmTokenByUserIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>())).ReturnsAsync("tok");
        var data = new Dictionary<string, string> { ["type"] = "BILL_REJECTED" };

        await _sut.SendToUserAsync(1, "T", "B", data);
        data["type"] = "MUTATED";

        Drain().Single().Data!["type"].Should().Be("BILL_REJECTED");
    }
}
