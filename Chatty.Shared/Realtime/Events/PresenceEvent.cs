using Chatty.Shared.Models.Enums;

namespace Chatty.Shared.Realtime.Events;

public sealed record PresenceEvent(
    Guid UserId,
    UserStatus Status,
    string? StatusMessage);
