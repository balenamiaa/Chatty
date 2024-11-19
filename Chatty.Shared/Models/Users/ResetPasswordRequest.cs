using MessagePack;

namespace Chatty.Shared.Models.Users;

[MessagePackObject]
public sealed record ResetPasswordRequest(
    [property: Key(0)] string Email,
    [property: Key(1)] string ResetToken,
    [property: Key(2)] string NewPassword);
