using System.Security.Claims;

using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Data.Models.Extensions;
using Chatty.Backend.Realtime;
using Chatty.Backend.Realtime.Events;
using Chatty.Backend.Realtime.Hubs;
using Chatty.Backend.Services.Channels;
using Chatty.Backend.Services.Messages;
using Chatty.Backend.Services.Presence;
using Chatty.Backend.Services.Servers;
using Chatty.Backend.Services.Voice;
using Chatty.Backend.Tests.Helpers;
using Chatty.Shared.Models.Channels;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Notifications;
using Chatty.Shared.Realtime.Events;
using Chatty.Shared.Realtime.Hubs;

using FluentValidation;

using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace Chatty.Backend.Tests.Realtime.Hubs;

public sealed class ChatHubTests : IDisposable
{
    private readonly ChattyDbContext _context;
    private readonly Mock<IPresenceService> _presenceService;
    private readonly Mock<IVoiceService> _voiceService;
    private readonly Mock<IConnectionTracker> _connectionTracker;
    private readonly Mock<IMessageService> _messageService;
    private readonly Mock<ITypingTracker> _typingTracker;
    private readonly Mock<IEventBus> _eventBus;
    private readonly Mock<IChannelService> _channelService;
    private readonly Mock<ILogger<ChatHub>> _logger;
    private readonly Mock<HubCallerContext> _hubCallerContext;
    private readonly Mock<IGroupManager> _groupManager;
    private readonly Mock<IHubCallerClients<IChatHubClient>> _hubClients;
    private readonly Mock<IServerService> _serverService;
    private readonly Mock<IValidator<NotificationPreferences>> _notificationSettingsValidator;
    private readonly ChatHub _sut;

    public ChatHubTests()
    {
        _context = TestDbContextFactory.Create();
        _presenceService = new Mock<IPresenceService>();
        _voiceService = new Mock<IVoiceService>();
        _connectionTracker = new Mock<IConnectionTracker>();
        _messageService = new Mock<IMessageService>();
        _typingTracker = new Mock<ITypingTracker>();
        _eventBus = new Mock<IEventBus>();
        _channelService = new Mock<IChannelService>();
        _logger = new Mock<ILogger<ChatHub>>();
        _serverService = new Mock<IServerService>();
        _notificationSettingsValidator = new Mock<IValidator<NotificationPreferences>>();

        _hubCallerContext = new Mock<HubCallerContext>();
        _groupManager = new Mock<IGroupManager>();
        _hubClients = new Mock<IHubCallerClients<IChatHubClient>>();

        _sut = new ChatHub(
            logger: _logger.Object,
            presenceService: _presenceService.Object,
            voiceService: _voiceService.Object,
            connectionTracker: _connectionTracker.Object,
            messageService: _messageService.Object,
            typingTracker: _typingTracker.Object,
            eventBus: _eventBus.Object,
            channelService: _channelService.Object,
            serverService: _serverService.Object,
            context: _context,
            notificationSettingsValidator: _notificationSettingsValidator.Object)
        {
            Context = _hubCallerContext.Object,
            Groups = _groupManager.Object,
            Clients = _hubClients.Object
        };

        SetupTestData();
    }

    private void SetupTestData()
    {
        TestData.TestDbSeeder.SeedBasicTestData(_context);
    }

    [Fact]
    public async Task OnConnectedAsync_AddsConnectionAndUpdatesPresence()
    {
        // Arrange
        var userId = TestData.Users.User1.Id;
        SetupUserContext(userId);

        // Act
        await _sut.OnConnectedAsync();

        // Assert
        _connectionTracker.Verify(x => x.AddConnectionAsync(
            userId,
            _hubCallerContext.Object.ConnectionId),
            Times.Once);

        _presenceService.Verify(x => x.UpdateLastSeenAsync(
            userId,
            It.IsAny<CancellationToken>()),
            Times.Once);

        _eventBus.Verify(x => x.PublishAsync(
            It.Is<OnlineStateEvent>(e =>
                e.UserId == userId &&
                e.IsOnline),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task OnDisconnectedAsync_RemovesConnectionAndUpdatesPresence()
    {
        // Arrange
        var userId = TestData.Users.User1.Id;
        SetupUserContext(userId);

        // Act
        await _sut.OnDisconnectedAsync(null);

        // Assert
        _connectionTracker.Verify(x => x.RemoveConnectionAsync(
            userId,
            _hubCallerContext.Object.ConnectionId),
            Times.Once);

        _presenceService.Verify(x => x.UpdateLastSeenAsync(
            userId,
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinChannelAsync_WithValidAccess_AddsToGroup()
    {
        // Arrange
        var userId = TestData.Users.User1.Id;
        var channelId = TestData.Channels.TextChannel.Id;
        SetupUserContext(userId);

        _channelService.Setup(x => x.GetAsync(channelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ChannelDto>.Success(TestData.Channels.TextChannel.ToDto()));

        _channelService.Setup(x => x.CanAccessAsync(userId, channelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        // Act
        await _sut.JoinChannelAsync(channelId);

        // Assert
        _groupManager.Verify(x => x.AddToGroupAsync(
            _hubCallerContext.Object.ConnectionId,
            $"channel_{channelId}",
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task JoinChannelAsync_WithPrivateChannel_EnforcesPermissions()
    {
        // Arrange
        var userId = TestData.Users.User1.Id;
        var channelId = TestData.Channels.PrivateChannel.Id;
        SetupUserContext(userId);

        _channelService.Setup(x => x.GetAsync(channelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<ChannelDto>.Success(TestData.Channels.PrivateChannel.ToDto()));
        _channelService.Setup(x => x.CanAccessAsync(userId, channelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(false));

        // Act & Assert
        await Assert.ThrowsAsync<HubException>(() => _sut.JoinChannelAsync(channelId));
        _groupManager.Verify(x => x.AddToGroupAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task StartTypingAsync_WithValidAccess_TracksAndPublishes()
    {
        // Arrange
        var userId = TestData.Users.User1.Id;
        var channelId = TestData.Channels.TextChannel.Id;
        SetupUserContext(userId);

        _channelService.Setup(x => x.CanAccessAsync(userId, channelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));

        _typingTracker.Setup(x => x.IsRateLimitedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        await _sut.StartTypingAsync(channelId);

        // Assert
        _typingTracker.Verify(x => x.TrackTypingAsync(
            channelId,
            userId,
            It.IsAny<CancellationToken>()),
            Times.Once);

        _eventBus.Verify(x => x.PublishAsync(
            It.Is<TypingEvent>(e =>
                e.ChannelId == channelId &&
                e.User.Id == userId &&
                e.IsTyping),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task StartTypingAsync_WhenRateLimited_ThrowsHubException()
    {
        // Arrange
        var userId = TestData.Users.User1.Id;
        var channelId = TestData.Channels.TextChannel.Id;
        SetupUserContext(userId);

        _channelService.Setup(x => x.CanAccessAsync(userId, channelId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(Result<bool>.Success(true));
        _typingTracker.Setup(x => x.IsRateLimitedAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act & Assert
        var ex = await Assert.ThrowsAsync<HubException>(() => _sut.StartTypingAsync(channelId));
        Assert.Equal("Too many typing indicators", ex.Message);
    }

    private void SetupUserContext(Guid userId)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userId.ToString())
        };
        var identity = new ClaimsIdentity(claims);
        var principal = new ClaimsPrincipal(identity);

        _hubCallerContext.Setup(x => x.User).Returns(principal);
        _hubCallerContext.Setup(x => x.ConnectionId).Returns(Guid.NewGuid().ToString());
    }

    public void Dispose()
    {
        TestDbContextFactory.Destroy(_context);
    }
}
