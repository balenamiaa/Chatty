using System.Security.Cryptography;
using System.Text;

namespace Chatty.Shared.Crypto;

public sealed class CryptoProvider : ICryptoProvider
{
    private const int NonceSize = 12; // 96 bits for AES-GCM
    private const int TagSize = 16; // 128 bits for AES-GCM authentication tag

    public byte[] GenerateKey()
    {
        return RandomNumberGenerator.GetBytes(32); // 256 bits
    }

    public byte[] GenerateNonce()
    {
        return RandomNumberGenerator.GetBytes(NonceSize);
    }

    public byte[] Encrypt(byte[] data, byte[] key, byte[] nonce)
    {
        if (key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes", nameof(key));

        if (nonce.Length != NonceSize)
            throw new ArgumentException($"Nonce must be {NonceSize} bytes", nameof(nonce));

        using var aes = new AesGcm(key, TagSize);
        var ciphertext = new byte[data.Length];
        var tag = new byte[TagSize];

        aes.Encrypt(nonce, data, ciphertext, tag);

        // Combine ciphertext and tag
        var result = new byte[ciphertext.Length + TagSize];
        Buffer.BlockCopy(ciphertext, 0, result, 0, ciphertext.Length);
        Buffer.BlockCopy(tag, 0, result, ciphertext.Length, TagSize);

        return result;
    }

    public byte[] Decrypt(byte[] data, byte[] key, byte[] nonce)
    {
        if (key.Length != 32)
            throw new ArgumentException("Key must be 32 bytes", nameof(key));

        if (nonce.Length != NonceSize)
            throw new ArgumentException($"Nonce must be {NonceSize} bytes", nameof(nonce));

        if (data.Length < TagSize)
            throw new ArgumentException("Data too short", nameof(data));

        using var aes = new AesGcm(key, TagSize);

        // Split ciphertext and tag
        var ciphertext = new byte[data.Length - TagSize];
        var tag = new byte[TagSize];
        Buffer.BlockCopy(data, 0, ciphertext, 0, data.Length - TagSize);
        Buffer.BlockCopy(data, data.Length - TagSize, tag, 0, TagSize);

        var plaintext = new byte[ciphertext.Length];
        aes.Decrypt(nonce, ciphertext, tag, plaintext);

        return plaintext;
    }

    public byte[] DeriveKey(byte[] masterKey, string context, int keyLength = 32)
    {
        var contextBytes = Encoding.UTF8.GetBytes(context);

        var prk = HKDF.Extract(HashAlgorithmName.SHA256, masterKey);
        var derivedKey = HKDF.Expand(HashAlgorithmName.SHA256, prk, keyLength, contextBytes);

        return derivedKey;
    }

    public byte[] Hash(byte[] data)
    {
        using var sha256 = SHA256.Create();
        return sha256.ComputeHash(data);
    }

    public string HashToString(byte[] data)
    {
        return Convert.ToHexString(Hash(data)).ToLowerInvariant();
    }
}
