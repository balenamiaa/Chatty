using Chatty.Shared.Realtime.Events;

namespace Chatty.Backend.Realtime.Events;

public sealed class EventBus : IEventBus
{
    private readonly Dictionary<Type, List<Func<object, Task>>> _handlers = new();
    private readonly Lock _lock = new();
    private readonly ILogger<EventBus> _logger;
    private readonly IEventDispatcher _eventDispatcher;

    public EventBus(
        ILogger<EventBus> logger,
        IEventDispatcher eventDispatcher)
    {
        _logger = logger;
        _eventDispatcher = eventDispatcher;

        // Subscribe to different event types
        SubscribeToMessageEvents();
        SubscribeToTypingEvents();
        SubscribeToPresenceEvents();
    }

    public Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default)
    {
        if (@event == null) throw new ArgumentNullException(nameof(@event));

        var eventType = typeof(TEvent);
        List<Func<object, Task>>? handlers;

        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out handlers))
                return Task.CompletedTask;

            handlers = handlers.ToList(); // Create a copy to avoid concurrent modification
        }

        var tasks = handlers.Select(handler =>
            ExecuteHandler(handler, @event));

        return Task.WhenAll(tasks);
    }

    private async Task ExecuteHandler(Func<object, Task> handler, object @event)
    {
        try
        {
            await handler(@event);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling event of type {EventType}", @event.GetType().Name);
        }
    }

    public IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler)
    {
        var eventType = typeof(TEvent);

        async Task WrapperHandler(object e) => await handler((TEvent)e);

        lock (_lock)
        {
            if (!_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers = new List<Func<object, Task>>();
                _handlers[eventType] = handlers;
            }

            handlers.Add(WrapperHandler);
        }

        return new Subscription(() => Unsubscribe(eventType, WrapperHandler));
    }

    private void Unsubscribe(Type eventType, Func<object, Task> handler)
    {
        lock (_lock)
        {
            if (_handlers.TryGetValue(eventType, out var handlers))
            {
                handlers.Remove(handler);
                if (!handlers.Any())
                    _handlers.Remove(eventType);
            }
        }
    }

    private void SubscribeToMessageEvents()
    {
        Subscribe<MessageEvent>(async e =>
            await _eventDispatcher.DispatchMessageReceivedAsync(e.ChannelId, e.Message));

        Subscribe<DirectMessageEvent>(async e =>
            await _eventDispatcher.DispatchDirectMessageReceivedAsync(e.Message));

        Subscribe<MessageDeletedEvent>(async e =>
            await _eventDispatcher.DispatchMessageDeletedAsync(e.ChannelId, e.MessageId));

        Subscribe<DirectMessageDeletedEvent>(async e =>
            await _eventDispatcher.DispatchDirectMessageDeletedAsync(e.MessageId));
    }

    private void SubscribeToTypingEvents()
    {
        Subscribe<TypingEvent>(async e =>
        {
            if (e.IsTyping)
                await _eventDispatcher.DispatchTypingStartedAsync(e.ChannelId, e.User);
            else
                await _eventDispatcher.DispatchTypingStoppedAsync(e.ChannelId, e.User);
        });

        Subscribe<DirectTypingEvent>(async e =>
        {
            if (e.IsTyping)
                await _eventDispatcher.DispatchDirectTypingStartedAsync(e.UserId, e.User);
            else
                await _eventDispatcher.DispatchDirectTypingStoppedAsync(e.UserId, e.User);
        });
    }

    private void SubscribeToPresenceEvents()
    {
        Subscribe<PresenceEvent>(async e =>
            await _eventDispatcher.DispatchUserPresenceChangedAsync(e.UserId, e.Status, e.StatusMessage));

        Subscribe<OnlineStateEvent>(async e =>
            await _eventDispatcher.DispatchUserOnlineStateChangedAsync(e.UserId, e.IsOnline));
    }

    private sealed class Subscription(Action unsubscribe) : IDisposable
    {
        private bool _disposed;

        public void Dispose()
        {
            if (_disposed) return;
            unsubscribe();
            _disposed = true;
        }
    }
}