using MessagePack;

namespace Chatty.Shared.Models.Users;

[MessagePackObject]
public sealed record RequestPasswordResetRequest(
    [property: Key(0)] string Email);
