namespace Chatty.Backend.Security.DeviceVerification;

public interface IDeviceVerificationService
{
    Task<string> GenerateVerificationCodeAsync(
        Guid userId,
        Guid deviceId,
        CancellationToken ct = default);

    Task<bool> VerifyDeviceAsync(
        Guid userId,
        Guid deviceId,
        string code,
        CancellationToken ct = default);

    Task<bool> IsDeviceVerifiedAsync(
        Guid userId,
        Guid deviceId,
        CancellationToken ct = default);

    Task<bool> RevokeDeviceVerificationAsync(
        Guid userId,
        Guid deviceId,
        CancellationToken ct = default);
}