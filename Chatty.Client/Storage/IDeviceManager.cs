namespace Chatty.Client.Storage;

/// <summary>
///     Platform-specific device management and secure storage
/// </summary>
public interface IDeviceManager
{
    /// <summary>
    ///     Gets or creates a unique device ID
    /// </summary>
    Task<Guid> GetOrCreateDeviceIdAsync();

    /// <summary>
    ///     Sets the device ID
    /// </summary>
    Task SetDeviceIdAsync(Guid deviceId);

    /// <summary>
    ///     Gets the device name
    /// </summary>
    Task<string> GetDeviceNameAsync();

    /// <summary>
    ///     Stores the device's private key securely
    /// </summary>
    Task StorePrivateKeyAsync(byte[] privateKey);

    /// <summary>
    ///     Retrieves the device's private key
    /// </summary>
    Task<byte[]> GetPrivateKeyAsync();

    /// <summary>
    ///     Stores the device's pre-key securely
    /// </summary>
    Task StorePreKeyAsync(byte[] preKey);

    /// <summary>
    ///     Retrieves the device's pre-key
    /// </summary>
    Task<byte[]> GetPreKeyAsync();

    /// <summary>
    ///     Stores a session key for a channel
    /// </summary>
    Task StoreChannelKeyAsync(Guid channelId, byte[] key, int version);

    /// <summary>
    ///     Retrieves a session key for a channel
    /// </summary>
    Task<(byte[] Key, int Version)?> GetChannelKeyAsync(Guid channelId);

    /// <summary>
    ///     Stores a session key for a direct message conversation
    /// </summary>
    Task StoreDirectMessageKeyAsync(Guid userId, Guid deviceId, byte[] key, int version);

    /// <summary>
    ///     Retrieves a session key for a direct message conversation
    /// </summary>
    Task<(byte[] Key, int Version)?> GetDirectMessageKeyAsync(Guid userId, Guid deviceId);

    /// <summary>
    ///     Clears all stored keys
    /// </summary>
    Task ClearKeysAsync();
}
