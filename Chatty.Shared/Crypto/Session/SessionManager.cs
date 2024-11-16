using System;
using System.Collections.Concurrent;
using System.Threading;

using Chatty.Shared.Crypto.KeyExchange;
using Chatty.Shared.Crypto.Session;

using Microsoft.Extensions.Logging;

namespace Chatty.Shared.Crypto.Session;

public sealed class SessionManager : ISessionManager
{
    private static readonly ConcurrentDictionary<(Guid UserId, Guid DeviceId), (byte[] Key, int Version)> _sessions = new();

    private readonly IKeyExchangeService _keyExchange;
    private readonly ICryptoProvider _crypto;
    private readonly ILogger<SessionManager> _logger;

    public SessionManager(
        IKeyExchangeService keyExchange,
        ICryptoProvider crypto,
        ILogger<SessionManager> logger)
    {
        _keyExchange = keyExchange;
        _crypto = crypto;
        _logger = logger;
    }

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
            var (ourPublicKey, ourPrivateKey) = await _keyExchange.GenerateKeyPairAsync();

            // Perform key exchange
            var sharedSecret = await _keyExchange.PerformKeyExchangeAsync(
                ourPrivateKey,
                publicKey,
                preKey);

            // Derive session key
            var salt = _crypto.GenerateNonce();
            var sessionKey = await _keyExchange.DeriveSessionKeyAsync(
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
            _logger.LogError(ex, "Failed to create session for user {UserId} device {DeviceId}", userId, deviceId);
            throw;
        }
    }

    public Task<bool> InvalidateSessionAsync(
        Guid userId,
        Guid deviceId,
        CancellationToken ct = default)
    {
        return Task.FromResult(_sessions.TryRemove((userId, deviceId), out _));
    }
}
