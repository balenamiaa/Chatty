using Chatty.Shared.Models.Enums;

using MessagePack;

namespace Chatty.Shared.Models.Users;

[MessagePackObject]
public sealed record UserDto(
    [property: Key(0)] Guid Id,
    [property: Key(1)] string Username,
    [property: Key(2)] string Email,
    [property: Key(3)] string? FirstName,
    [property: Key(4)] string? LastName,
    [property: Key(5)] string? ProfilePictureUrl,
    [property: Key(6)] string? StatusMessage,
    [property: Key(7)] UserStatus Status,
    [property: Key(8)] DateTime? LastOnlineAt,
    [property: Key(9)] string Locale,
    [property: Key(10)] DateTime CreatedAt);
