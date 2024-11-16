using System.Collections.Concurrent;

using Chatty.Backend.Data;

using Microsoft.EntityFrameworkCore;

namespace Chatty.Backend.Security.DeviceVerification;

public sealed class DeviceVerificationService(
    ChattyDbContext context,
    ILogger<DeviceVerificationService> logger)
    : IDeviceVerificationService
{
    private static readonly ConcurrentDictionary<(Guid UserId, Guid DeviceId), string> _pendingVerifications = new();
    private static readonly ConcurrentDictionary<(Guid UserId, Guid DeviceId), bool> _verifiedDevices = new();

    private readonly ILogger<DeviceVerificationService> _logger = logger;

    public async Task<string> GenerateVerificationCodeAsync(
        Guid userId,
        Guid deviceId,
        CancellationToken ct = default)
    {
        var device = await context.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId, ct);

        if (device is null)
            throw new InvalidOperationException("Device not found");

        // Generate a 6-digit code
        var code = Random.Shared.Next(100000, 999999).ToString();

        _pendingVerifications.AddOrUpdate(
            (userId, deviceId),
            code,
            (_, _) => code);

        return code;
    }

    public async Task<bool> VerifyDeviceAsync(
        Guid userId,
        Guid deviceId,
        string code,
        CancellationToken ct = default)
    {
        var device = await context.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == userId && d.DeviceId == deviceId, ct);

        if (device is null)
            return false;

        if (!_pendingVerifications.TryGetValue((userId, deviceId), out var storedCode))
            return false;

        if (code != storedCode)
            return false;

        // Remove pending verification and mark device as verified
        _pendingVerifications.TryRemove((userId, deviceId), out _);
        _verifiedDevices.AddOrUpdate((userId, deviceId), true, (_, _) => true);

        return true;
    }

    public Task<bool> IsDeviceVerifiedAsync(
        Guid userId,
        Guid deviceId,
        CancellationToken ct = default)
    {
        return Task.FromResult(
            _verifiedDevices.TryGetValue((userId, deviceId), out var isVerified) && isVerified);
    }

    public Task<bool> RevokeDeviceVerificationAsync(
        Guid userId,
        Guid deviceId,
        CancellationToken ct = default)
    {
        _pendingVerifications.TryRemove((userId, deviceId), out _);
        return Task.FromResult(_verifiedDevices.TryRemove((userId, deviceId), out _));
    }
}
