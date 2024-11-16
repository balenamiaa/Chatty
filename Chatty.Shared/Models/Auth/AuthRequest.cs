namespace Chatty.Shared.Models.Auth;

public sealed record AuthRequest(
    string Email,
    string Password,
    string? DeviceId = null,
    string? DeviceName = null);
