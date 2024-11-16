using System.Linq.Expressions;
using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Data.Models.Extensions;
using Chatty.Backend.Realtime;
using Chatty.Backend.Realtime.Events;
using Chatty.Backend.Realtime.Hubs;
using Chatty.Backend.Tests.Helpers;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Realtime.Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Chatty.Backend.Tests.Realtime.Events;

public sealed class EventDispatcherTests : IDisposable
{
    private readonly ChattyDbContext _context;
    private readonly Mock<IHubContext<ChatHub, IChatHubClient>> _hubContext;
    private readonly Mock<IConnectionTracker> _connectionTracker;
    private readonly Mock<ILogger<EventDispatcher>> _logger;
    private readonly EventDispatcher _sut;

    public EventDispatcherTests()
    {
        _context = TestDbContextFactory.Create();
        _hubContext = TestHubContextHelper.CreateHubContext<ChatHub, IChatHubClient>();
        _connectionTracker = new Mock<IConnectionTracker>();
        _logger = new Mock<ILogger<EventDispatcher>>();

        _sut = new EventDispatcher(
            _hubContext.Object,
            _connectionTracker.Object,
            _logger.Object,
            _context);

        SetupTestData();
    }

    [Fact]
    public async Task DispatchMessageReceivedAsync_NotifiesChannelAndMembers()
    {
        // Arrange
        var channelId = TestData.Channel1.Id;
        var message = TestData.Message1.ToDto();
        var connectionId = "connection1";

        _connectionTracker.Setup(x => x.GetConnectionsAsync(It.IsAny<Guid>()))
            .ReturnsAsync(new[] { connectionId });

        // Mock the hub context clients
        var mockClients = new Mock<IHubClients<IChatHubClient>>();
        var mockClientProxy = new Mock<IChatHubClient>();

        mockClients.Setup(c => c.Group(It.IsAny<string>()))
            .Returns(mockClientProxy.Object);
        mockClients.Setup(c => c.Clients(It.IsAny<IReadOnlyList<string>>()))
            .Returns(mockClientProxy.Object);

        _hubContext.Setup(x => x.Clients).Returns(mockClients.Object);

        // Setup channel members
        var channelMember = new ChannelMember
        {
            ChannelId = channelId,
            UserId = TestData.User1.Id
        };
        _context.ChannelMembers.Add(channelMember);
        await _context.SaveChangesAsync();

        // Act
        await _sut.DispatchMessageReceivedAsync(channelId, message);

        // Assert
        mockClientProxy.Verify(x => x.OnMessageReceived(channelId, message), Times.Once);
        mockClientProxy.Verify(x => x.OnNotification(
            "New Message",
            $"New message in channel from {message.Sender.Username}"),
            Times.Once);
    }

    [Fact]
    public async Task DispatchTypingStartedAsync_NotifiesChannel()
    {
        // Arrange
        var channelId = TestData.Channel1.Id;
        var user = TestData.User1.ToDto();

        // Act
        await _sut.DispatchTypingStartedAsync(channelId, user);

        // Assert
        VerifyHubClientCall(x => x.OnTypingStarted(channelId, user));
    }

    [Fact]
    public async Task DispatchUserPresenceChangedAsync_NotifiesAllClients()
    {
        // Arrange
        var userId = TestData.User1.Id;
        var status = UserStatus.Online;
        var statusMessage = "Available";

        // Act
        await _sut.DispatchUserPresenceChangedAsync(userId, status, statusMessage);

        // Assert
        VerifyHubClientCall(x => x.OnUserPresenceChanged(userId, status, statusMessage));
    }

    private void VerifyHubClientCall(Expression<Action<IChatHubClient>> clientAction)
    {
        var clientProxy = Mock.Get(_hubContext.Object.Clients.All);
        clientProxy.Verify(clientAction, Times.Once);
    }

    private void SetupTestData()
    {
        _context.Users.Add(TestData.User1);
        _context.Users.Add(TestData.User2);
        _context.Channels.Add(TestData.Channel1);
        _context.Messages.Add(TestData.Message1);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        TestDbContextFactory.Destroy(_context);
    }

    private static class TestData
    {
        public static readonly User User1 = new()
        {
            Id = Guid.NewGuid(),
            Username = "testuser1",
            Email = "test1@example.com",
            PasswordHash = "hash"
        };

        public static readonly User User2 = new()
        {
            Id = Guid.NewGuid(),
            Username = "testuser2",
            Email = "test2@example.com",
            PasswordHash = "hash"
        };

        public static readonly Channel Channel1 = new()
        {
            Id = Guid.NewGuid(),
            Name = "test-channel",
            ChannelType = ChannelType.Text
        };

        public static readonly Message Message1 = new()
        {
            Id = Guid.NewGuid(),
            ChannelId = Channel1.Id,
            SenderId = User1.Id,
            Content = [1, 2, 3],
            ContentType = ContentType.Text,
            MessageNonce = [4, 5, 6],
            KeyVersion = 1
        };
    }
}