using Microsoft.Extensions.Logging;

namespace Chatty.Client.Storage;

/// <summary>
///     Platform-specific device management and secure storage implementation
/// </summary>
public sealed class DeviceManager(
    ILogger<DeviceManager> logger,
    ISecureStorage storage) : IDeviceManager
{
    private const string DeviceIdKey = "device_id";
    private const string DeviceNameKey = "device_name";
    private const string PrivateKeyKey = "device_private_key";
    private const string PreKeyKey = "device_pre_key";
    private const string ChannelKeyPrefix = "channel_key_";
    private const string DirectMessageKeyPrefix = "dm_key_";

    private readonly ILogger<DeviceManager> _logger = logger;

    public async Task<Guid> GetOrCreateDeviceIdAsync()
    {
        var storedId = await storage.GetAsync(DeviceIdKey);
        if (!string.IsNullOrEmpty(storedId) && Guid.TryParse(storedId, out var deviceId))
        {
            return deviceId;
        }

        // Generate new device ID
        deviceId = Guid.NewGuid();
        await storage.SetAsync(DeviceIdKey, deviceId.ToString());
        return deviceId;
    }

    public async Task SetDeviceIdAsync(Guid deviceId) => await storage.SetAsync(DeviceIdKey, deviceId.ToString());

    public async Task<string> GetDeviceNameAsync()
    {
        var name = await storage.GetAsync(DeviceNameKey);
        return !string.IsNullOrEmpty(name) ? name : Environment.MachineName;
    }

    public async Task StorePrivateKeyAsync(byte[] privateKey) => await storage.SetBytesAsync(PrivateKeyKey, privateKey);

    public async Task<byte[]> GetPrivateKeyAsync() => await storage.GetBytesAsync(PrivateKeyKey) ?? [];

    public async Task StorePreKeyAsync(byte[] preKey) => await storage.SetBytesAsync(PreKeyKey, preKey);

    public async Task<byte[]> GetPreKeyAsync() => await storage.GetBytesAsync(PreKeyKey) ?? [];

    public async Task StoreChannelKeyAsync(Guid channelId, byte[] key, int version)
    {
        var keyData = new byte[key.Length + sizeof(int)];
        Buffer.BlockCopy(key, 0, keyData, 0, key.Length);
        Buffer.BlockCopy(BitConverter.GetBytes(version), 0, keyData, key.Length, sizeof(int));

        await storage.SetBytesAsync($"{ChannelKeyPrefix}{channelId}", keyData);
    }

    public async Task<(byte[] Key, int Version)?> GetChannelKeyAsync(Guid channelId)
    {
        var keyData = await storage.GetBytesAsync($"{ChannelKeyPrefix}{channelId}");
        if (keyData == null || keyData.Length < sizeof(int))
        {
            return null;
        }

        var key = new byte[keyData.Length - sizeof(int)];
        Buffer.BlockCopy(keyData, 0, key, 0, key.Length);

        var version = BitConverter.ToInt32(keyData, key.Length);
        return (key, version);
    }

    public async Task StoreDirectMessageKeyAsync(Guid userId, Guid deviceId, byte[] key, int version)
    {
        var keyData = new byte[key.Length + sizeof(int)];
        Buffer.BlockCopy(key, 0, keyData, 0, key.Length);
        Buffer.BlockCopy(BitConverter.GetBytes(version), 0, keyData, key.Length, sizeof(int));

        await storage.SetBytesAsync($"{DirectMessageKeyPrefix}{userId}_{deviceId}", keyData);
    }

    public async Task<(byte[] Key, int Version)?> GetDirectMessageKeyAsync(Guid userId, Guid deviceId)
    {
        var keyData = await storage.GetBytesAsync($"{DirectMessageKeyPrefix}{userId}_{deviceId}");
        if (keyData == null || keyData.Length < sizeof(int))
        {
            return null;
        }

        var key = new byte[keyData.Length - sizeof(int)];
        Buffer.BlockCopy(keyData, 0, key, 0, key.Length);

        var version = BitConverter.ToInt32(keyData, key.Length);
        return (key, version);
    }

    public async Task ClearKeysAsync()
    {
        await storage.RemoveAsync(PrivateKeyKey);
        await storage.RemoveAsync(PreKeyKey);

        // Clear all channel keys
        var channelKeys = await storage.GetAllKeysAsync(ChannelKeyPrefix);
        foreach (var key in channelKeys)
        {
            await storage.RemoveAsync(key);
        }

        // Clear all direct message keys
        var dmKeys = await storage.GetAllKeysAsync(DirectMessageKeyPrefix);
        foreach (var key in dmKeys)
        {
            await storage.RemoveAsync(key);
        }
    }
}
