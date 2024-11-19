using MessagePack;

namespace Chatty.Shared.Models.Users;

[MessagePackObject]
public record UpdateProfileRequest(
    [property: Key(0)] string? FirstName,
    [property: Key(1)] string? LastName,
    [property: Key(2)] string? StatusMessage,
    [property: Key(3)] string? ProfilePictureUrl,
    [property: Key(4)] string? Locale
);
