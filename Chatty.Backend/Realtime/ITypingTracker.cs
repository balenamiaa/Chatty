namespace Chatty.Backend.Realtime;

public interface ITypingTracker
{
    Task TrackTypingAsync(Guid channelId, Guid userId, CancellationToken ct = default);
    Task TrackDirectTypingAsync(Guid userId, Guid recipientId, CancellationToken ct = default);
    Task<IReadOnlyList<Guid>> GetTypingUsersAsync(Guid channelId, CancellationToken ct = default);
    Task<bool> IsUserTypingAsync(Guid userId, Guid recipientId, CancellationToken ct = default);
}