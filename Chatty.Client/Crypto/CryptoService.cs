using System.Security.Cryptography;
using System.Text;

using Chatty.Client.Storage;

using Microsoft.Extensions.Logging;

namespace Chatty.Client.Crypto;

/// <summary>
///     Implementation of the cryptographic service for E2E encryption
/// </summary>
public class CryptoService(
    IDeviceManager deviceManager,
    ILogger<CryptoService> logger)
    : ICryptoService
{
    private const int TagSize = 16; // AES-GCM authentication tag size
    private readonly ILogger<CryptoService> _logger = logger;

    public Task<(byte[] PublicKey, byte[] PrivateKey)> GenerateKeyPairAsync()
    {
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        return Task.FromResult((
            ecdh.PublicKey.ExportSubjectPublicKeyInfo(),
            ecdh.ExportPkcs8PrivateKey()
        ));
    }

    public Task<byte[]> PerformKeyExchangeAsync(
        byte[] ourPrivateKey,
        byte[] theirPublicKey,
        byte[] preKey)
    {
        using var ourEcdh = ECDiffieHellman.Create();
        ourEcdh.ImportPkcs8PrivateKey(ourPrivateKey, out _);

        using var theirEcdh = ECDiffieHellman.Create();
        theirEcdh.ImportSubjectPublicKeyInfo(theirPublicKey, out _);

        using var preKeyEcdh = ECDiffieHellman.Create();
        preKeyEcdh.ImportSubjectPublicKeyInfo(preKey, out _);

        // Combine shared secrets
        var sharedSecret1 = ourEcdh.DeriveKeyMaterial(theirEcdh.PublicKey);
        var sharedSecret2 = ourEcdh.DeriveKeyMaterial(preKeyEcdh.PublicKey);

        var combinedSecret = new byte[sharedSecret1.Length + sharedSecret2.Length];
        Buffer.BlockCopy(sharedSecret1, 0, combinedSecret, 0, sharedSecret1.Length);
        Buffer.BlockCopy(sharedSecret2, 0, combinedSecret, sharedSecret1.Length, sharedSecret2.Length);

        return Task.FromResult(combinedSecret);
    }

    public Task<byte[]> DeriveSessionKeyAsync(
        byte[] sharedSecret,
        byte[] salt,
        string info,
        int keyLength = 32)
    {
        var infoBytes = Encoding.UTF8.GetBytes(info);
        var prk = HKDF.Extract(HashAlgorithmName.SHA256, sharedSecret, salt);
        var sessionKey = HKDF.Expand(HashAlgorithmName.SHA256, prk, keyLength, infoBytes);
        return Task.FromResult(sessionKey);
    }

    public async Task<byte[]> EncryptAsync(byte[] data, byte[] key)
    {
        if (key.Length != 32)
        {
            throw new ArgumentException("Key must be 32 bytes", nameof(key));
        }

        var nonce = await GenerateNonceAsync();
        using var aes = new AesGcm(key, TagSize);
        var ciphertext = new byte[data.Length];
        var tag = new byte[TagSize];

        aes.Encrypt(nonce, data, ciphertext, tag);

        // Combine nonce, ciphertext, and tag
        var result = new byte[nonce.Length + ciphertext.Length + TagSize];
        Buffer.BlockCopy(nonce, 0, result, 0, nonce.Length);
        Buffer.BlockCopy(ciphertext, 0, result, nonce.Length, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, nonce.Length + ciphertext.Length, TagSize);

        return result;
    }

    public Task<byte[]> DecryptAsync(byte[] data, byte[] key)
    {
        if (key.Length != 32)
        {
            throw new ArgumentException("Key must be 32 bytes", nameof(key));
        }

        if (data.Length < 12 + TagSize) // Nonce + Tag size
        {
            throw new ArgumentException("Data too short", nameof(data));
        }

        // Extract nonce, ciphertext, and tag
        var nonce = new byte[12];
        Buffer.BlockCopy(data, 0, nonce, 0, 12);

        var ciphertext = new byte[data.Length - 12 - TagSize];
        Buffer.BlockCopy(data, 12, ciphertext, 0, ciphertext.Length);

        var tag = new byte[TagSize];
        Buffer.BlockCopy(data, data.Length - TagSize, tag, 0, TagSize);

        using var aes = new AesGcm(key, TagSize);
        var plaintext = new byte[ciphertext.Length];
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return Task.FromResult(plaintext);
    }

    public Task<byte[]> GenerateKeyAsync(int length = 32) => Task.FromResult(RandomNumberGenerator.GetBytes(length));

    public Task<byte[]> GenerateNonceAsync(int length = 12) => Task.FromResult(RandomNumberGenerator.GetBytes(length));

    public Task<byte[]> HashAsync(byte[] data)
    {
        using var sha256 = SHA256.Create();
        return Task.FromResult(sha256.ComputeHash(data));
    }

    public Task<byte[]> DeriveKeyAsync(byte[] masterKey, string context, int keyLength = 32)
    {
        var contextBytes = Encoding.UTF8.GetBytes(context);
        var prk = HKDF.Extract(HashAlgorithmName.SHA256, masterKey);
        var derivedKey = HKDF.Expand(HashAlgorithmName.SHA256, prk, keyLength, contextBytes);
        return Task.FromResult(derivedKey);
    }

    public async Task<byte[]> EncryptMessageAsync(byte[] message, Guid channelId, int keyVersion)
    {
        // Get or create channel key
        var channelKey = await deviceManager.GetChannelKeyAsync(channelId);
        if (channelKey is null || channelKey.Value.Version != keyVersion)
        {
            throw new InvalidOperationException($"No valid key found for channel {channelId} version {keyVersion}");
        }

        return await EncryptAsync(message, channelKey.Value.Key);
    }

    public async Task<byte[]> DecryptMessageAsync(byte[] encryptedMessage, Guid channelId, int keyVersion)
    {
        // Get channel key
        var channelKey = await deviceManager.GetChannelKeyAsync(channelId);
        if (channelKey is null || channelKey.Value.Version != keyVersion)
        {
            throw new InvalidOperationException($"No valid key found for channel {channelId} version {keyVersion}");
        }

        return await DecryptAsync(encryptedMessage, channelKey.Value.Key);
    }

    public async Task<byte[]> EncryptDirectMessageAsync(
        byte[] message,
        Guid userId,
        Guid deviceId,
        int keyVersion)
    {
        // Get or create direct message key
        var dmKey = await deviceManager.GetDirectMessageKeyAsync(userId, deviceId);
        if (dmKey is null || dmKey.Value.Version != keyVersion)
        {
            throw new InvalidOperationException(
                $"No valid key found for DM with user {userId} device {deviceId} version {keyVersion}");
        }

        return await EncryptAsync(message, dmKey.Value.Key);
    }

    public async Task<byte[]> DecryptDirectMessageAsync(
        byte[] encryptedMessage,
        Guid userId,
        Guid deviceId,
        int keyVersion)
    {
        // Get direct message key
        var dmKey = await deviceManager.GetDirectMessageKeyAsync(userId, deviceId);
        if (dmKey is null || dmKey.Value.Version != keyVersion)
        {
            throw new InvalidOperationException(
                $"No valid key found for DM with user {userId} device {deviceId} version {keyVersion}");
        }

        return await DecryptAsync(encryptedMessage, dmKey.Value.Key);
    }
}
