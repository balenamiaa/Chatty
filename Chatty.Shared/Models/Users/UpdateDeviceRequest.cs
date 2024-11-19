using MessagePack;

namespace Chatty.Shared.Models.Users;

[MessagePackObject]
public record UpdateDeviceRequest(
    [property: Key(0)] string? Name,
    [property: Key(1)] string? PushToken
);
