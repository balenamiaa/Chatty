namespace Chatty.Backend.Realtime.Events;

public interface IEventBus
{
    Task PublishAsync<TEvent>(TEvent @event, CancellationToken ct = default);
    IDisposable Subscribe<TEvent>(Func<TEvent, Task> handler);
}