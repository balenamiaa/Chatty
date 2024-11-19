namespace Chatty.Client.Storage;

/// <summary>
///     Platform-specific secure storage interface
/// </summary>
public interface ISecureStorage
{
    /// <summary>
    ///     Gets a value from secure storage
    /// </summary>
    Task<string?> GetAsync(string key);

    /// <summary>
    ///     Sets a value in secure storage
    /// </summary>
    Task SetAsync(string key, string value);

    /// <summary>
    ///     Gets a byte array from secure storage
    /// </summary>
    Task<byte[]?> GetBytesAsync(string key);

    /// <summary>
    ///     Sets a byte array in secure storage
    /// </summary>
    Task SetBytesAsync(string key, byte[] value);

    /// <summary>
    ///     Removes a value from secure storage
    /// </summary>
    Task RemoveAsync(string key);

    /// <summary>
    ///     Gets all keys with a given prefix
    /// </summary>
    Task<IEnumerable<string>> GetAllKeysAsync(string prefix);
}
