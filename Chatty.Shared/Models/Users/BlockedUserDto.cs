using MessagePack;

namespace Chatty.Shared.Models.Users;

[MessagePackObject]
public record BlockedUserDto(
    [property: Key(0)] Guid Id,
    [property: Key(1)] Guid UserId,
    [property: Key(2)] Guid BlockedUserId,
    [property: Key(3)] UserDto BlockedUser,
    [property: Key(4)] string? Reason,
    [property: Key(5)] DateTime CreatedAt
);
