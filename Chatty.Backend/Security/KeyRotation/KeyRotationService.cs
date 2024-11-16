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
        var currentVersion = _currentVersions.GetOrAdd(userId, 1);
        var newVersion = currentVersion + 1;
        var newKey = crypto.GenerateKey();

        // Store both old and new keys
        var userKeys = _userKeys.GetOrAdd(userId, _ => new ConcurrentDictionary<int, byte[]>());
        userKeys.TryAdd(newVersion, newKey);

        // Keep last 3 versions
        var oldVersions = userKeys.Keys.OrderByDescending(k => k).Skip(3);
        foreach (var version in oldVersions)
        {
            userKeys.TryRemove(version, out _);
        }

        _currentVersions.TryUpdate(userId, newVersion, currentVersion);

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