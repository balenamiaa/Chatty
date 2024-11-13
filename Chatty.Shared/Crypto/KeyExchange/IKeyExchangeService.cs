namespace Chatty.Shared.Crypto.KeyExchange;

public interface IKeyExchangeService
{
    // Key generation
    Task<(byte[] PublicKey, byte[] PrivateKey)> GenerateKeyPairAsync();
    Task<byte[]> GeneratePreKeyAsync(byte[] privateKey);

    // Key exchange
    Task<byte[]> PerformKeyExchangeAsync(
        byte[] ourPrivateKey,
        byte[] theirPublicKey,
        byte[] preKey);

    // Session keys
    Task<byte[]> DeriveSessionKeyAsync(
        byte[] sharedSecret,
        byte[] salt,
        string info,
        int keyLength = 32);
}