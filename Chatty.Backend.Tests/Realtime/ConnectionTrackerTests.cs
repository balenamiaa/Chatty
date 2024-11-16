using Chatty.Backend.Realtime;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Chatty.Backend.Tests.Realtime;

public sealed class ConnectionTrackerTests
{
    private readonly Mock<ILogger<ConnectionTracker>> _logger;
    private readonly ConnectionTracker _sut;

    public ConnectionTrackerTests()
    {
        _logger = new Mock<ILogger<ConnectionTracker>>();
        _sut = new ConnectionTracker(_logger.Object);
    }

    [Fact]
    public async Task AddConnectionAsync_AddsConnection()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var connectionId = "connection1";

        // Act
        await _sut.AddConnectionAsync(userId, connectionId);
        var connections = await _sut.GetConnectionsAsync(userId);

        // Assert
        Assert.Single(connections);
        Assert.Contains(connectionId, connections);
    }

    [Fact]
    public async Task RemoveConnectionAsync_RemovesConnection()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var connectionId = "connection1";
        await _sut.AddConnectionAsync(userId, connectionId);

        // Act
        await _sut.RemoveConnectionAsync(userId, connectionId);
        var connections = await _sut.GetConnectionsAsync(userId);

        // Assert
        Assert.Empty(connections);
    }

    [Fact]
    public async Task IsOnlineAsync_ReturnsTrueWhenUserHasConnections()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var connectionId = "connection1";
        await _sut.AddConnectionAsync(userId, connectionId);

        // Act
        var isOnline = await _sut.IsOnlineAsync(userId);

        // Assert
        Assert.True(isOnline);
    }

    [Fact]
    public async Task IsOnlineAsync_ReturnsFalseWhenUserHasNoConnections()
    {
        // Arrange
        var userId = Guid.NewGuid();

        // Act
        var isOnline = await _sut.IsOnlineAsync(userId);

        // Assert
        Assert.False(isOnline);
    }

    [Fact]
    public async Task GetConnectionsAsync_ReturnsAllUserConnections()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var connectionIds = new[] { "connection1", "connection2", "connection3" };

        foreach (var connectionId in connectionIds)
        {
            await _sut.AddConnectionAsync(userId, connectionId);
        }

        // Act
        var connections = await _sut.GetConnectionsAsync(userId);

        // Assert
        Assert.Equal(connectionIds.Length, connections.Count);
        Assert.All(connectionIds, id => Assert.Contains(id, connections));
    }

    [Fact]
    public async Task GetConnectionsAsync_WithMultipleUsers_ReturnsCorrectConnections()
    {
        // Arrange
        var user1 = Guid.NewGuid();
        var user2 = Guid.NewGuid();
        await _sut.AddConnectionAsync(user1, "connection1");
        await _sut.AddConnectionAsync(user2, "connection2");

        // Act
        var result = await _sut.GetConnectionsAsync(new[] { user1, user2 });

        // Assert
        Assert.Equal(2, result.Count);
        Assert.Contains("connection1", result[user1]);
        Assert.Contains("connection2", result[user2]);
    }
}