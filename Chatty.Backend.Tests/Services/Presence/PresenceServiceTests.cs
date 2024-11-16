using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Realtime;
using Chatty.Backend.Realtime.Events;
using Chatty.Backend.Services.Presence;
using Chatty.Backend.Tests.Helpers;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Realtime.Events;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Chatty.Backend.Tests.Services.Presence;

public sealed class PresenceServiceTests : IDisposable
{
    private readonly IDbContextFactory<ChattyDbContext> _contextFactory;
    private readonly Mock<IConnectionTracker> _connectionTracker;
    private readonly Mock<IEventBus> _eventBus;
    private readonly Mock<ILogger<PresenceService>> _logger;
    private readonly PresenceService _sut;

    public PresenceServiceTests()
    {
        _contextFactory = TestDbContextFactory.CreateFactory();
        _connectionTracker = new Mock<IConnectionTracker>();
        _eventBus = new Mock<IEventBus>();
        _logger = new Mock<ILogger<PresenceService>>();

        _sut = new PresenceService(
            _contextFactory,
            _connectionTracker.Object,
            _eventBus.Object,
            _logger.Object);

        SetupTestData();
    }

    [Fact]
    public async Task UpdateStatusAsync_WithValidStatus_ReturnsSuccess()
    {
        // Arrange
        var userId = TestData.Users.User1.Id;
        var status = UserStatus.DoNotDisturb;
        var statusMessage = "In a meeting";

        // Act
        var result = await _sut.UpdateStatusAsync(userId, status, statusMessage);

        // Assert
        Assert.True(result.IsSuccess);

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Verify database update
        var user = await context.Users.FindAsync(userId);
        Assert.Equal(statusMessage, user!.StatusMessage);

        // Verify event was published
        _eventBus.Verify(x => x.PublishAsync(
            It.Is<PresenceEvent>(e =>
                e.UserId == userId &&
                e.Status == status &&
                e.StatusMessage == statusMessage),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task UpdateLastSeenAsync_WhenOnline_UpdatesTimestamp()
    {
        // Arrange
        var userId = TestData.Users.User1.Id;
        var previousLastSeen = DateTime.UtcNow.AddHours(-1);

        // Setup initial state
        await using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var user = await context.Users.FindAsync(userId);
            user!.LastOnlineAt = previousLastSeen;
            await context.SaveChangesAsync();
        }

        _connectionTracker.Setup(x => x.IsOnlineAsync(userId))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.UpdateLastSeenAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify with fresh context
        await using (var context = await _contextFactory.CreateDbContextAsync())
        {
            var user = await context.Users.FindAsync(userId);
            Assert.True(user!.LastOnlineAt > previousLastSeen);
        }
    }

    [Fact]
    public async Task GetUserStatusAsync_ReturnsCorrectStatus()
    {
        // Arrange
        var userId = TestData.Users.User1.Id;
        _connectionTracker.Setup(x => x.IsOnlineAsync(userId))
            .ReturnsAsync(true);

        // Act
        var result = await _sut.GetUserStatusAsync(userId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Online, result.Value);
    }

    [Fact]
    public async Task GetUsersStatusAsync_ReturnsMultipleStatuses()
    {
        // Arrange
        var user1 = TestData.Users.User1.Id;
        var user2 = TestData.Users.User2.Id;

        _connectionTracker.Setup(x => x.GetConnectionsAsync(It.IsAny<IEnumerable<Guid>>()))
            .ReturnsAsync(new Dictionary<Guid, IReadOnlyList<string>>
            {
                { user1, new[] { "connection1" } },
                { user2, Array.Empty<string>() }
            });

        // Act
        var result = await _sut.GetUsersStatusAsync(new[] { user1, user2 });

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(UserStatus.Online, result.Value[user1]);
        Assert.Equal(UserStatus.Offline, result.Value[user2]);
    }

    private void SetupTestData()
    {
        using var context = _contextFactory.CreateDbContext();
        TestData.TestDbSeeder.SeedBasicTestData(context);
    }

    public void Dispose()
    {
        TestDbContextFactory.Destroy(_contextFactory);
    }
}