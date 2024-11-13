namespace Chatty.Shared.Crypto;

public interface ICryptoProvider
{
    // Key generation
    byte[] GenerateKey();
    byte[] GenerateNonce();

    // Symmetric encryption (AES-GCM)
    byte[] Encrypt(byte[] data, byte[] key, byte[] nonce);
    byte[] Decrypt(byte[] data, byte[] key, byte[] nonce);

    // Key derivation
    byte[] DeriveKey(byte[] masterKey, string context, int keyLength = 32);

    // Hashing
    byte[] Hash(byte[] data);
    string HashToString(byte[] data);
}