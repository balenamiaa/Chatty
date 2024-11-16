namespace Chatty.Backend.Security.KeyRotation;

public interface IKeyRotationService
{
    Task<byte[]> GetCurrentKeyAsync(Guid userId, CancellationToken ct = default);
    Task<(byte[] Key, int Version)> RotateKeyAsync(Guid userId, CancellationToken ct = default);
    Task<byte[]> GetKeyByVersionAsync(Guid userId, int version, CancellationToken ct = default);
    Task<IReadOnlyDictionary<int, byte[]>> GetAllKeysAsync(Guid userId, CancellationToken ct = default);
    Task<bool> RevokeKeyAsync(Guid userId, int version, CancellationToken ct = default);
}
