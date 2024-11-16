using Chatty.Shared.Models.Enums;

namespace Chatty.Shared.Models.Devices;

public sealed record RegisterDeviceRequest(
    Guid DeviceId,
    string? DeviceName,
    DeviceType DeviceType,
    string? DeviceToken,
    byte[] PublicKey);
