using Chatty.Shared.Models.Enums;

namespace Chatty.Shared.Realtime.Events;

public sealed record PresenceEvent(
    Guid UserId,
    UserStatus Status,
    string? StatusMessage);

public sealed record OnlineStateEvent(
    Guid UserId,
    bool IsOnline);