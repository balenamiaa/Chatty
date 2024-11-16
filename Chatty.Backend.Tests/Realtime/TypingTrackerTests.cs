using Chatty.Backend.Realtime;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Chatty.Backend.Tests.Realtime;

public sealed class TypingTrackerTests
{
    private readonly Mock<ILogger<TypingTracker>> _logger;
    private readonly TypingTracker _sut;

    public TypingTrackerTests()
    {
        _logger = new Mock<ILogger<TypingTracker>>();
        _sut = new TypingTracker(_logger.Object);
    }

    [Fact]
    public async Task TrackTypingAsync_AddsUserToChannel()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        // Act
        await _sut.TrackTypingAsync(channelId, userId);
        var typingUsers = await _sut.GetTypingUsersAsync(channelId);

        // Assert
        Assert.Single(typingUsers);
        Assert.Contains(userId, typingUsers);
    }

    [Fact]
    public async Task GetTypingUsersAsync_ExpiresOldEntries()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        await _sut.TrackTypingAsync(channelId, userId);

        // Act
        await Task.Delay(6000); // Wait for expiration (5 seconds)
        var typingUsers = await _sut.GetTypingUsersAsync(channelId);

        // Assert
        Assert.Empty(typingUsers);
    }

    [Fact]
    public async Task TrackDirectTypingAsync_TracksUserTyping()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();

        // Act
        await _sut.TrackDirectTypingAsync(userId, recipientId);
        var isTyping = await _sut.IsUserTypingAsync(userId, recipientId);

        // Assert
        Assert.True(isTyping);
    }

    [Fact]
    public async Task IsUserTypingAsync_ReturnsFalseAfterExpiration()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var recipientId = Guid.NewGuid();
        await _sut.TrackDirectTypingAsync(userId, recipientId);

        // Act
        await Task.Delay(6000); // Wait for expiration (5 seconds)
        var isTyping = await _sut.IsUserTypingAsync(userId, recipientId);

        // Assert
        Assert.False(isTyping);
    }

    [Fact]
    public async Task IsRateLimitedAsync_EnforcesRateLimit()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        await _sut.TrackTypingAsync(Guid.NewGuid(), userId);
        var isRateLimited = await _sut.IsRateLimitedAsync(userId);

        // Assert
        Assert.True(isRateLimited);
    }

    [Fact]
    public async Task IsRateLimitedAsync_AllowsAfterCooldown()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await _sut.TrackTypingAsync(Guid.NewGuid(), userId);

        // Act
        await Task.Delay(1100); // Wait for rate limit cooldown (1 second)
        var isRateLimited = await _sut.IsRateLimitedAsync(userId);

        // Assert
        Assert.False(isRateLimited);
    }
}