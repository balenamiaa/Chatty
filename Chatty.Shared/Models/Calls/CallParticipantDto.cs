using Chatty.Shared.Models.Users;

using MessagePack;

namespace Chatty.Shared.Models.Calls;

[MessagePackObject]
public sealed record CallParticipantDto(
    [property: Key(0)] Guid Id,
    [property: Key(1)] Guid CallId,
    [property: Key(2)] UserDto? User,
    [property: Key(3)] DateTime JoinedAt,
    [property: Key(4)] DateTime? LeftAt,
    [property: Key(5)] bool Muted,
    [property: Key(6)] bool VideoEnabled);
