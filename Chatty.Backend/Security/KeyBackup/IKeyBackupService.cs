namespace Chatty.Backend.Security.KeyBackup;

public interface IKeyBackupService
{
    Task<string> CreateBackupAsync(Guid userId, byte[] masterKey, CancellationToken ct = default);
    Task<byte[]> RestoreBackupAsync(Guid userId, string backupData, CancellationToken ct = default);
    Task<bool> VerifyBackupAsync(Guid userId, string backupData, CancellationToken ct = default);
    Task<bool> RevokeBackupAsync(Guid userId, CancellationToken ct = default);
}