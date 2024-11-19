using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

using Chatty.Shared.Crypto;

namespace Chatty.Backend.Security.KeyBackup;

public sealed class KeyBackupService(
    ICryptoProvider crypto,
    ILogger<KeyBackupService> logger)
    : IKeyBackupService
{
    private static readonly ConcurrentDictionary<Guid, byte[]> _backupKeys = new();

    public Task<string> CreateBackupAsync(
        Guid userId,
        byte[] masterKey,
        CancellationToken ct = default)
    {
        try
        {
            // Generate backup key
            var backupKey = crypto.GenerateKey();
            _backupKeys.AddOrUpdate(userId, backupKey, (_, _) => backupKey);

            // Create backup data
            var backupData = new BackupData
            {
                UserId = userId,
                MasterKey = masterKey,
                CreatedAt = DateTime.UtcNow
            };

            // Encrypt backup
            var json = JsonSerializer.Serialize(backupData);
            var data = Encoding.UTF8.GetBytes(json);
            var nonce = crypto.GenerateNonce();
            var encrypted = crypto.Encrypt(data, backupKey, nonce);

            // Combine nonce and encrypted data
            var result = new byte[nonce.Length + encrypted.Length];
            Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
            Buffer.BlockCopy(encrypted, 0, result, nonce.Length, encrypted.Length);

            return Task.FromResult(Convert.ToBase64String(result));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create backup for user {UserId}", userId);
            throw;
        }
    }

    public Task<byte[]> RestoreBackupAsync(
        Guid userId,
        string backupData,
        CancellationToken ct = default)
    {
        try
        {
            if (!_backupKeys.TryGetValue(userId, out var backupKey))
            {
                throw new InvalidOperationException("Backup key not found");
            }

            // Decode backup data
            var combined = Convert.FromBase64String(backupData);
            var nonce = combined.AsSpan(0, 12).ToArray();
            var encrypted = combined.AsSpan(12).ToArray();

            // Decrypt backup
            var decrypted = crypto.Decrypt(encrypted, backupKey, nonce);
            var json = Encoding.UTF8.GetString(decrypted);
            var backup = JsonSerializer.Deserialize<BackupData>(json);

            if (backup?.UserId != userId)
            {
                throw new InvalidOperationException("Invalid backup data");
            }

            return Task.FromResult(backup.MasterKey);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to restore backup for user {UserId}", userId);
            throw;
        }
    }

    public async Task<bool> VerifyBackupAsync(
        Guid userId,
        string backupData,
        CancellationToken ct = default)
    {
        try
        {
            await RestoreBackupAsync(userId, backupData, ct);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public Task<bool> RevokeBackupAsync(
        Guid userId,
        CancellationToken ct = default) =>
        Task.FromResult(_backupKeys.TryRemove(userId, out _));

    private class BackupData
    {
        public required Guid UserId { get; init; }
        public required byte[] MasterKey { get; init; }
        public required DateTime CreatedAt { get; init; }
    }
}
