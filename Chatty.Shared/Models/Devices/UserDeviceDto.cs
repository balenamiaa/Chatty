using Chatty.Shared.Models.Enums;

using MessagePack;

namespace Chatty.Shared.Models.Devices;

[MessagePackObject]
public sealed record UserDeviceDto(
    [property: Key(0)] Guid Id,
    [property: Key(1)] Guid UserId,
    [property: Key(2)] string Name,
    [property: Key(3)] DeviceType DeviceType,
    [property: Key(4)] string? PushToken,
    [property: Key(5)] DateTime LastActiveAt,
    [property: Key(6)] DateTime CreatedAt);
