using MessagePack;

namespace Chatty.Shared.Models.Users;

[MessagePackObject]
public record FriendDto(
    [property: Key(0)] Guid Id,
    [property: Key(1)] Guid UserId,
    [property: Key(2)] Guid FriendId,
    [property: Key(3)] UserDto Friend,
    [property: Key(4)] bool IsPending,
    [property: Key(5)] bool IsBlocked,
    [property: Key(6)] DateTime CreatedAt
);
