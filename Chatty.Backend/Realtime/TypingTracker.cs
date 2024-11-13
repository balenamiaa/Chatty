using System.Collections.Concurrent;

namespace Chatty.Backend.Realtime;

public sealed class TypingTracker(ILogger<TypingTracker> logger) : ITypingTracker
{
    private static readonly TimeSpan TypingTimeout = TimeSpan.FromSeconds(5);
    private readonly ConcurrentDictionary<Guid, ConcurrentDictionary<Guid, DateTime>> _channelTyping = new();
    private readonly ConcurrentDictionary<(Guid, Guid), DateTime> _directTyping = new();
    private readonly ILogger<TypingTracker> _logger = logger;

    public Task TrackTypingAsync(Guid channelId, Guid userId, CancellationToken ct = default)
    {
        var channelUsers = _channelTyping.GetOrAdd(channelId, _ => new ConcurrentDictionary<Guid, DateTime>());
        channelUsers.AddOrUpdate(userId, DateTime.UtcNow, (_, _) => DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public Task TrackDirectTypingAsync(Guid userId, Guid recipientId, CancellationToken ct = default)
    {
        _directTyping.AddOrUpdate((userId, recipientId), DateTime.UtcNow, (_, _) => DateTime.UtcNow);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyList<Guid>> GetTypingUsersAsync(Guid channelId, CancellationToken ct = default)
    {
        if (_channelTyping.TryGetValue(channelId, out var channelUsers))
        {
            var now = DateTime.UtcNow;
            var typingUsers = channelUsers
                .Where(x => now - x.Value <= TypingTimeout)
                .Select(x => x.Key)
                .ToList();

            return Task.FromResult<IReadOnlyList<Guid>>(typingUsers);
        }

        return Task.FromResult<IReadOnlyList<Guid>>(Array.Empty<Guid>());
    }

    public Task<bool> IsUserTypingAsync(Guid userId, Guid recipientId, CancellationToken ct = default)
    {
        if (_directTyping.TryGetValue((userId, recipientId), out var lastTyped))
        {
            return Task.FromResult(DateTime.UtcNow - lastTyped <= TypingTimeout);
        }

        return Task.FromResult(false);
    }
}