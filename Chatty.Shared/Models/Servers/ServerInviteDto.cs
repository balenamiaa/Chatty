using Chatty.Shared.Models.Users;

using MessagePack;

namespace Chatty.Shared.Models.Servers;

[MessagePackObject]
public record ServerInviteDto(
    [property: Key(0)] Guid Id,
    [property: Key(1)] Guid ServerId,
    [property: Key(2)] Guid CreatorId,
    [property: Key(3)] UserDto Creator,
    [property: Key(4)] string Code,
    [property: Key(5)] int MaxUses,
    [property: Key(6)] int Uses,
    [property: Key(7)] DateTime? ExpiresAt,
    [property: Key(8)] DateTime CreatedAt
);
