using System.Security.Cryptography;
using Chatty.Shared.Crypto;
using System.Text;
using Xunit;

namespace Chatty.Backend.Tests.Security;

public sealed class CryptoProviderTests
{
    private readonly ICryptoProvider _sut;

    public CryptoProviderTests()
    {
        _sut = new CryptoProvider();
    }

    [Fact]
    public void GenerateKey_ReturnsValidKey()
    {
        // Act
        var key = _sut.GenerateKey();

        // Assert
        Assert.Equal(32, key.Length); // 256 bits
        Assert.NotEqual(new byte[32], key); // Not all zeros
    }

    [Fact]
    public void GenerateNonce_ReturnsValidNonce()
    {
        // Act
        var nonce = _sut.GenerateNonce();

        // Assert
        Assert.Equal(12, nonce.Length); // 96 bits
        Assert.NotEqual(new byte[12], nonce); // Not all zeros
    }

    [Fact]
    public void EncryptDecrypt_RoundTrip_ReturnsOriginalData()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("Hello, World!");
        var key = _sut.GenerateKey();
        var nonce = _sut.GenerateNonce();

        // Act
        var encrypted = _sut.Encrypt(data, key, nonce);
        var decrypted = _sut.Decrypt(encrypted, key, nonce);

        // Assert
        Assert.Equal(data, decrypted);
    }

    [Fact]
    public void DeriveKey_WithSameInputs_ReturnsSameKey()
    {
        // Arrange
        var masterKey = _sut.GenerateKey();
        var context = "test-context";

        // Act
        var key1 = _sut.DeriveKey(masterKey, context);
        var key2 = _sut.DeriveKey(masterKey, context);

        // Assert
        Assert.Equal(key1, key2);
    }

    [Fact]
    public void DeriveKey_WithDifferentContexts_ReturnsDifferentKeys()
    {
        // Arrange
        var masterKey = _sut.GenerateKey();

        // Act
        var key1 = _sut.DeriveKey(masterKey, "context1");
        var key2 = _sut.DeriveKey(masterKey, "context2");

        // Assert
        Assert.NotEqual(key1, key2);
    }

    [Fact]
    public void Hash_WithSameInput_ReturnsSameHash()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("test data");

        // Act
        var hash1 = _sut.Hash(data);
        var hash2 = _sut.Hash(data);

        // Assert
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void HashToString_ReturnsValidHexString()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("test data");

        // Act
        var hashString = _sut.HashToString(data);

        // Assert
        Assert.Matches("^[0-9a-f]{64}$", hashString); // SHA-256 produces 32 bytes = 64 hex chars
    }

    [Theory]
    [InlineData(16)]
    [InlineData(64)]
    public void DeriveKey_WithCustomLength_ReturnsCorrectLength(int keyLength)
    {
        // Arrange
        var masterKey = _sut.GenerateKey();
        var context = "test-context";

        // Act
        var derivedKey = _sut.DeriveKey(masterKey, context, keyLength);

        // Assert
        Assert.Equal(keyLength, derivedKey.Length);
    }

    [Fact]
    public void Encrypt_WithInvalidKeySize_ThrowsArgumentException()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("test data");
        var invalidKey = new byte[16]; // Wrong size
        var nonce = _sut.GenerateNonce();

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sut.Encrypt(data, invalidKey, nonce));
    }

    [Fact]
    public void Encrypt_WithInvalidNonceSize_ThrowsArgumentException()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("test data");
        var key = _sut.GenerateKey();
        var invalidNonce = new byte[16]; // Wrong size

        // Act & Assert
        Assert.Throws<ArgumentException>(() => _sut.Encrypt(data, key, invalidNonce));
    }

    [Fact]
    public void Encrypt_WithDifferentNonce_ProducesDifferentCiphertext()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("test data");
        var key = _sut.GenerateKey();
        var nonce1 = _sut.GenerateNonce();
        var nonce2 = _sut.GenerateNonce();

        // Act
        var encrypted1 = _sut.Encrypt(data, key, nonce1);
        var encrypted2 = _sut.Encrypt(data, key, nonce2);

        // Assert
        Assert.NotEqual(encrypted1, encrypted2);
    }

    [Fact]
    public void Decrypt_WithWrongKey_ThrowsAuthenticationTagMismatchException()
    {
        // Arrange
        var data = Encoding.UTF8.GetBytes("test data");
        var key1 = _sut.GenerateKey();
        var key2 = _sut.GenerateKey();
        var nonce = _sut.GenerateNonce();
        var encrypted = _sut.Encrypt(data, key1, nonce);

        // Act & Assert
        Assert.Throws<AuthenticationTagMismatchException>(() => _sut.Decrypt(encrypted, key2, nonce));
    }
}