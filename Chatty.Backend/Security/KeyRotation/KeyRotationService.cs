using System.Collections.Concurrent;
using Chatty.Shared.Crypto;

namespace Chatty.Backend.Security.KeyRotation;

public sealed class KeyRotationService(
    ICryptoProvider crypto,
    ILogger<KeyRotationService> logger)
    : IKeyRotationService
{
    private static readonly ConcurrentDictionary<Guid, ConcurrentDictionary<int, byte[]>> _userKeys = new();
    private static readonly ConcurrentDictionary<Guid, int> _currentVersions = new();

    public Task<byte[]> GetCurrentKeyAsync(Guid userId, CancellationToken ct = default)
    {
        var version = _currentVersions.GetOrAdd(userId, 1);
        var userKeys = _userKeys.GetOrAdd(userId, _ => new ConcurrentDictionary<int, byte[]>());

        if (userKeys.TryGetValue(version, out var key))
        {
            return Task.FromResult(key);
        }

        // Generate initial key if none exists
        key = crypto.GenerateKey();
        userKeys.TryAdd(version, key);

        return Task.FromResult(key);
    }

    public async Task<(byte[] Key, int Version)> RotateKeyAsync(Guid userId, CancellationToken ct = default)
    {
        var userKeys = _userKeys.GetOrAdd(userId, _ => new ConcurrentDictionary<int, byte[]>());
        var currentVersion = _currentVersions.GetOrAdd(userId, 1);
        var newVersion = currentVersion + 1;

        // Generate new key
        var newKey = crypto.GenerateKey();
        userKeys.TryAdd(newVersion, newKey);
        _currentVersions.TryUpdate(userId, newVersion, currentVersion);

        logger.LogInformation("Rotated key for user {UserId} to version {Version}", userId, newVersion);

        return (newKey, newVersion);
    }

    public Task<byte[]> GetKeyByVersionAsync(Guid userId, int version, CancellationToken ct = default)
    {
        var userKeys = _userKeys.GetOrAdd(userId, _ => new ConcurrentDictionary<int, byte[]>());

        if (userKeys.TryGetValue(version, out var key))
        {
            return Task.FromResult(key);
        }

        throw new KeyNotFoundException($"Key version {version} not found for user {userId}");
    }

    public Task<IReadOnlyDictionary<int, byte[]>> GetAllKeysAsync(Guid userId, CancellationToken ct = default)
    {
        var userKeys = _userKeys.GetOrAdd(userId, _ => new ConcurrentDictionary<int, byte[]>());
        return Task.FromResult<IReadOnlyDictionary<int, byte[]>>(
            new Dictionary<int, byte[]>(userKeys));
    }

    public Task<bool> RevokeKeyAsync(Guid userId, int version, CancellationToken ct = default)
    {
        var userKeys = _userKeys.GetOrAdd(userId, _ => new ConcurrentDictionary<int, byte[]>());
        return Task.FromResult(userKeys.TryRemove(version, out _));
    }
}