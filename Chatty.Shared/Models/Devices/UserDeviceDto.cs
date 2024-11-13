using Chatty.Shared.Models.Enums;

namespace Chatty.Shared.Models.Devices;

public sealed record UserDeviceDto(
    Guid Id,
    Guid UserId,
    Guid DeviceId,
    string? DeviceName,
    DeviceType DeviceType,
    string? DeviceToken,
    DateTime LastActiveAt,
    DateTime CreatedAt);