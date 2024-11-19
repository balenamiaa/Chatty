namespace Chatty.Client.Exceptions;

/// <summary>
///     Exception thrown when a cryptographic operation fails
/// </summary>
public class CryptoException(
    string message,
    string code = "CRYPTO_ERROR",
    Exception? innerException = null)
    : ChattyException(message, code, innerException)
{
    public static CryptoException KeyNotFound(string keyType, string identifier) =>
        new(
            $"No {keyType} key found for {identifier}",
            "KEY_NOT_FOUND");

    public static CryptoException KeyVersionMismatch(
        string keyType,
        string identifier,
        int expectedVersion,
        int actualVersion) =>
        new(
            $"Version mismatch for {keyType} key {identifier}: expected {expectedVersion}, got {actualVersion}",
            "KEY_VERSION_MISMATCH");

    public static CryptoException DecryptionFailed(string message, Exception? innerException = null) =>
        new(
            $"Failed to decrypt data: {message}",
            "DECRYPTION_FAILED",
            innerException);

    public static CryptoException EncryptionFailed(string message, Exception? innerException = null) =>
        new(
            $"Failed to encrypt data: {message}",
            "ENCRYPTION_FAILED",
            innerException);

    public static CryptoException KeyExchangeFailed(string message, Exception? innerException = null) =>
        new(
            $"Key exchange failed: {message}",
            "KEY_EXCHANGE_FAILED",
            innerException);
}
