using System.Security.Cryptography;

namespace Chatty.Shared.Crypto.KeyExchange;

public sealed class KeyExchangeService : IKeyExchangeService
{
    private const int KeySize = 32; // 256 bits

    public Task<(byte[] PublicKey, byte[] PrivateKey)> GenerateKeyPairAsync()
    {
        using var ecdh = ECDiffieHellman.Create(ECCurve.NamedCurves.nistP256);
        return Task.FromResult((
            ecdh.PublicKey.ExportSubjectPublicKeyInfo(),
            ecdh.ExportPkcs8PrivateKey()
        ));
    }

    public Task<byte[]> GeneratePreKeyAsync(byte[] privateKey)
    {
        using var ecdh = ECDiffieHellman.Create();
        ecdh.ImportPkcs8PrivateKey(privateKey, out _);
        return Task.FromResult(ecdh.PublicKey.ExportSubjectPublicKeyInfo());
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
        var pseudorandomKey = HKDF.Extract(HashAlgorithmName.SHA256, sharedSecret, salt);
        var sessionKey = HKDF.Expand(HashAlgorithmName.SHA256, pseudorandomKey, keyLength, System.Text.Encoding.UTF8.GetBytes(info));

        return Task.FromResult(sessionKey);
    }
}