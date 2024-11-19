using System.Net.Http.Json;

using Chatty.Client.Crypto;
using Chatty.Client.Storage;
using Chatty.Shared.Models.Channels;

namespace Chatty.Client.Services.Channels;

/// <summary>
///     Manages channel key operations including sharing and rotation
/// </summary>
public class ChannelKeyManager(
    ICryptoService cryptoService,
    IDeviceManager deviceManager,
    HttpClient httpClient)
{
    /// <summary>
    ///     Creates a new channel key and shares it with initial members
    /// </summary>
    public async Task<byte[]> CreateChannelKeyAsync(
        Guid channelId,
        IEnumerable<ChannelMemberDto> initialMembers,
        CancellationToken ct = default)
    {
        // Generate new channel key
        var key = await cryptoService.GenerateKeyAsync();
        var version = 1;

        // Store locally
        await deviceManager.StoreChannelKeyAsync(channelId, key, version);

        // Share with initial members
        await ShareChannelKeyAsync(channelId, key, version, initialMembers, ct);

        return key;
    }

    /// <summary>
    ///     Rotates the channel key and shares it with all current members
    /// </summary>
    public async Task<byte[]> RotateChannelKeyAsync(
        Guid channelId,
        IEnumerable<ChannelMemberDto> currentMembers,
        CancellationToken ct = default)
    {
        // Get current key info
        var currentKey = await deviceManager.GetChannelKeyAsync(channelId);
        if (currentKey is null)
        {
            throw new InvalidOperationException($"No key found for channel {channelId}");
        }

        // Generate new key
        var newKey = await cryptoService.GenerateKeyAsync();
        var newVersion = currentKey.Value.Version + 1;

        // Store locally
        await deviceManager.StoreChannelKeyAsync(channelId, newKey, newVersion);

        // Share with members
        await ShareChannelKeyAsync(channelId, newKey, newVersion, currentMembers, ct);

        return newKey;
    }

    /// <summary>
    ///     Shares the channel key with a new member
    /// </summary>
    public async Task ShareChannelKeyWithMemberAsync(
        Guid channelId,
        ChannelMemberDto member,
        CancellationToken ct = default)
    {
        // Get current key
        var currentKey = await deviceManager.GetChannelKeyAsync(channelId);
        if (currentKey is null)
        {
            throw new InvalidOperationException($"No key found for channel {channelId}");
        }

        // Share with member
        await ShareChannelKeyAsync(
            channelId,
            currentKey.Value.Key,
            currentKey.Value.Version,
            [member],
            ct);
    }

    private async Task ShareChannelKeyAsync(
        Guid channelId,
        byte[] key,
        int version,
        IEnumerable<ChannelMemberDto> members,
        CancellationToken ct)
    {
        // Get our private key for key exchange
        var ourPrivateKey = await deviceManager.GetPrivateKeyAsync();

        // Share key with each member's devices
        foreach (var member in members)
        {
            // Get member's devices
            var response = await httpClient.GetAsync(
                $"api/users/{member.User.Id}/devices",
                ct);

            if (!response.IsSuccessStatusCode)
            {
                // TODO: Better error handling
                continue;
            }

            var devices = await response.Content.ReadFromJsonAsync<List<DeviceDto>>(
                ct);

            if (devices is null)
            {
                continue;
            }

            // Share key with each device
            foreach (var device in devices)
            {
                try
                {
                    // Perform key exchange
                    var sharedSecret = await cryptoService.PerformKeyExchangeAsync(
                        ourPrivateKey,
                        Convert.FromBase64String(device.PublicKey),
                        Convert.FromBase64String(device.PreKeyPublic));

                    // Derive session key
                    var salt = await cryptoService.GenerateNonceAsync();
                    var sessionKey = await cryptoService.DeriveSessionKeyAsync(
                        sharedSecret,
                        salt,
                        $"channel_key:{channelId}:{device.Id}");

                    // Encrypt channel key
                    var encryptedKey = await cryptoService.EncryptAsync(key, sessionKey);

                    // Send to server
                    var request = new ShareChannelKeyRequest(
                        member.User.Id,
                        device.Id,
                        channelId,
                        version,
                        Convert.ToBase64String(encryptedKey),
                        Convert.ToBase64String(salt));

                    await httpClient.PostAsJsonAsync(
                        "api/channels/keys/share",
                        request,
                        ct);
                }
                catch
                {
                    // TODO: Better error handling
                }
            }
        }
    }
}

internal record DeviceDto(
    Guid Id,
    string Name,
    string PublicKey,
    string PreKeyPublic);

internal record ShareChannelKeyRequest(
    Guid UserId,
    Guid DeviceId,
    Guid ChannelId,
    int Version,
    string EncryptedKey,
    string Salt);
