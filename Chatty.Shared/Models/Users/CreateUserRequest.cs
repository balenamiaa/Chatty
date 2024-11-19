using MessagePack;

namespace Chatty.Shared.Models.Users;

[MessagePackObject]
public sealed record CreateUserRequest(
    [property: Key(0)] string Username,
    [property: Key(1)] string Email,
    [property: Key(2)] string Password,
    [property: Key(3)] string? FirstName,
    [property: Key(4)] string? LastName,
    [property: Key(5)] string? ProfilePictureUrl,
    [property: Key(6)] string Locale);
