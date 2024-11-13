namespace Chatty.Shared.Crypto.Session;

public interface ISessionManager
{
    Task<byte[]> GetSessionKeyAsync(
        Guid userId,
        Guid deviceId,
        int keyVersion,
        CancellationToken ct = default);

    Task<byte[]> CreateSessionAsync(
        Guid userId,
        Guid deviceId,
        byte[] publicKey,
        byte[] preKey,
        CancellationToken ct = default);

    Task<bool> InvalidateSessionAsync(
        Guid userId,
        Guid deviceId,
        CancellationToken ct = default);
}