using Chatty.Backend.Realtime.Events;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Models.Users;
using Chatty.Shared.Realtime.Events;

using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace Chatty.Backend.Tests.Realtime.Events;

public sealed class EventBusTests
{
    private readonly Mock<IEventDispatcher> _eventDispatcher;
    private readonly Mock<ILogger<EventBus>> _logger;
    private readonly EventBus _sut;

    public EventBusTests()
    {
        _logger = new Mock<ILogger<EventBus>>();
        _eventDispatcher = new Mock<IEventDispatcher>();
        _sut = new EventBus(_logger.Object, _eventDispatcher.Object);
    }

    [Fact]
    public async Task PublishAsync_WithMessageEvent_DispatchesToSubscribers()
    {
        // Arrange
        var channelId = Guid.NewGuid();
        var message = new MessageDto(
            Guid.NewGuid(),
            channelId,
            new UserDto(
                Guid.NewGuid(),
                "test",
                "test@example.com",
                null,
                null,
                null,
                null,
                UserStatus.Online,
                null,
                "en-US",
                DateTime.UtcNow),
            [1, 2, 3],
            ContentType.Text,
            DateTime.UtcNow,
            DateTime.UtcNow,
            false,
            [4, 5, 6],
            1,
            null,
            0,
            [],
            []);

        var @event = new MessageEvent(channelId, message);

        // Act
        await _sut.PublishAsync(@event);

        // Assert
        _eventDispatcher.Verify(x => x.DispatchMessageReceivedAsync(
                channelId,
                message),
            Times.Once);
    }

    [Fact]
    public async Task PublishAsync_WithPresenceEvent_DispatchesToSubscribers()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var status = UserStatus.Online;
        var statusMessage = "Available";
        var @event = new PresenceEvent(userId, status, statusMessage);

        // Act
        await _sut.PublishAsync(@event);

        // Assert
        _eventDispatcher.Verify(x => x.DispatchUserPresenceChangedAsync(
                userId,
                status,
                statusMessage),
            Times.Once);
    }

    [Fact]
    public async Task Subscribe_HandlesMultipleSubscriptions()
    {
        // Arrange
        var receivedEvents = new List<string>();
        var subscription1 = _sut.Subscribe<TestEvent>(e =>
        {
            receivedEvents.Add("subscriber1");
            return Task.CompletedTask;
        });
        var subscription2 = _sut.Subscribe<TestEvent>(e =>
        {
            receivedEvents.Add("subscriber2");
            return Task.CompletedTask;
        });

        // Act
        await _sut.PublishAsync(new TestEvent());

        // Assert
        Assert.Equal(2, receivedEvents.Count);
        Assert.Contains("subscriber1", receivedEvents);
        Assert.Contains("subscriber2", receivedEvents);
    }

    [Fact]
    public async Task Subscribe_UnsubscribeRemovesHandler()
    {
        // Arrange
        var eventReceived = false;
        var subscription = _sut.Subscribe<TestEvent>(e =>
        {
            eventReceived = true;
            return Task.CompletedTask;
        });

        // Act
        subscription.Dispose();
        await _sut.PublishAsync(new TestEvent());

        // Assert
        Assert.False(eventReceived);
    }

    [Fact]
    public async Task PublishAsync_HandlesFailedSubscriber()
    {
        // Arrange
        var goodSubscriberCalled = false;
        _sut.Subscribe<TestEvent>(_ => throw new Exception("Test exception"));
        _sut.Subscribe<TestEvent>(_ =>
        {
            goodSubscriberCalled = true;
            return Task.CompletedTask;
        });

        // Act
        await _sut.PublishAsync(new TestEvent());

        // Assert
        Assert.True(goodSubscriberCalled);
    }

    private class TestEvent
    {
    }
}
