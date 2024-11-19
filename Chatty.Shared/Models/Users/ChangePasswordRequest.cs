using MessagePack;

namespace Chatty.Shared.Models.Users;

[MessagePackObject]
public sealed record ChangePasswordRequest(
    [property: Key(0)] string CurrentPassword,
    [property: Key(1)] string NewPassword);
