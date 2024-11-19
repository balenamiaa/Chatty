using System.Collections.Concurrent;

using Chatty.Shared.Crypto.KeyExchange;

using Microsoft.Extensions.Logging;

namespace Chatty.Shared.Crypto.Session;

public sealed class SessionManager(
    IKeyExchangeService keyExchange,
    ICryptoProvider crypto,
    ILogger<SessionManager> logger)
    : ISessionManager
{
    private static readonly ConcurrentDictionary<(Guid UserId, Guid DeviceId), (byte[] Key, int Version)> _sessions =
        new();

    public Task<byte[]> GetSessionKeyAsync(
        Guid userId,
        Guid deviceId,
        int keyVersion,
        CancellationToken ct = default)
    {
        if (_sessions.TryGetValue((userId, deviceId), out var session) && session.Version == keyVersion)
        {
            return Task.FromResult(session.Key);
        }

        throw new InvalidOperationException("Session not found or key version mismatch");
    }

    public async Task<byte[]> CreateSessionAsync(
        Guid userId,
        Guid deviceId,
        byte[] publicKey,
        byte[] preKey,
        CancellationToken ct = default)
    {
        try
        {
            // Generate our keypair
            var (ourPublicKey, ourPrivateKey) = await keyExchange.GenerateKeyPairAsync();

            // Perform key exchange
            var sharedSecret = await keyExchange.PerformKeyExchangeAsync(
                ourPrivateKey,
                publicKey,
                preKey);

            // Derive session key
            var salt = crypto.GenerateNonce();
            var sessionKey = await keyExchange.DeriveSessionKeyAsync(
                sharedSecret,
                salt,
                $"session:{userId}:{deviceId}");

            // Store session
            _sessions.AddOrUpdate(
                (userId, deviceId),
                (sessionKey, 1),
                (_, _) => (sessionKey, 1));

            return sessionKey;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create session for user {UserId} device {DeviceId}", userId, deviceId);
            throw;
        }
    }

    public Task<bool> InvalidateSessionAsync(
        Guid userId,
        Guid deviceId,
        CancellationToken ct = default) =>
        Task.FromResult(_sessions.TryRemove((userId, deviceId), out _));
}
