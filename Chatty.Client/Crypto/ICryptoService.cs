namespace Chatty.Client.Crypto;

/// <summary>
///     Service for handling end-to-end encryption
/// </summary>
public interface ICryptoService
{
    /// <summary>
    ///     Generates a new key pair for asymmetric encryption
    /// </summary>
    Task<(byte[] PublicKey, byte[] PrivateKey)> GenerateKeyPairAsync();

    /// <summary>
    ///     Performs key exchange with another user's device
    /// </summary>
    Task<byte[]> PerformKeyExchangeAsync(
        byte[] ourPrivateKey,
        byte[] theirPublicKey,
        byte[] preKey);

    /// <summary>
    ///     Derives a session key from a shared secret
    /// </summary>
    Task<byte[]> DeriveSessionKeyAsync(
        byte[] sharedSecret,
        byte[] salt,
        string info,
        int keyLength = 32);

    /// <summary>
    ///     Encrypts data using AES-GCM
    /// </summary>
    Task<byte[]> EncryptAsync(byte[] data, byte[] key);

    /// <summary>
    ///     Decrypts data using AES-GCM
    /// </summary>
    Task<byte[]> DecryptAsync(byte[] data, byte[] key);

    /// <summary>
    ///     Generates a random key
    /// </summary>
    Task<byte[]> GenerateKeyAsync(int length = 32);

    /// <summary>
    ///     Generates a random nonce
    /// </summary>
    Task<byte[]> GenerateNonceAsync(int length = 12);

    /// <summary>
    ///     Computes a SHA-256 hash of data
    /// </summary>
    Task<byte[]> HashAsync(byte[] data);

    /// <summary>
    ///     Derives a key from a master key
    /// </summary>
    Task<byte[]> DeriveKeyAsync(byte[] masterKey, string context, int keyLength = 32);

    /// <summary>
    ///     Encrypts a message for a specific channel
    /// </summary>
    Task<byte[]> EncryptMessageAsync(
        byte[] message,
        Guid channelId,
        int keyVersion);

    /// <summary>
    ///     Decrypts a message from a specific channel
    /// </summary>
    Task<byte[]> DecryptMessageAsync(
        byte[] encryptedMessage,
        Guid channelId,
        int keyVersion);

    /// <summary>
    ///     Encrypts a direct message for a specific user's device
    /// </summary>
    Task<byte[]> EncryptDirectMessageAsync(
        byte[] message,
        Guid userId,
        Guid deviceId,
        int keyVersion);

    /// <summary>
    ///     Decrypts a direct message from a specific user's device
    /// </summary>
    Task<byte[]> DecryptDirectMessageAsync(
        byte[] encryptedMessage,
        Guid userId,
        Guid deviceId,
        int keyVersion);
}
