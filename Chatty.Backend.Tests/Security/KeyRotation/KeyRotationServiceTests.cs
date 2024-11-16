using Chatty.Backend.Security.KeyRotation;
using Chatty.Shared.Crypto;

using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace Chatty.Backend.Tests.Security.KeyRotation;

public sealed class KeyRotationServiceTests
{
    private readonly Mock<ICryptoProvider> _crypto;
    private readonly Mock<ILogger<KeyRotationService>> _logger;
    private readonly KeyRotationService _sut;

    public KeyRotationServiceTests()
    {
        _crypto = new Mock<ICryptoProvider>();
        _logger = new Mock<ILogger<KeyRotationService>>();
        _sut = new KeyRotationService(_crypto.Object, _logger.Object);

        // Setup default crypto behavior
        _crypto.Setup(x => x.GenerateKey())
            .Returns(() => new byte[32]); // Return empty key for testing
    }

    [Fact]
    public async Task GetCurrentKeyAsync_ForNewUser_GeneratesKey()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var expectedKey = new byte[32];
        _crypto.Setup(x => x.GenerateKey()).Returns(expectedKey);

        // Act
        var key = await _sut.GetCurrentKeyAsync(userId);

        // Assert
        Assert.Equal(expectedKey, key);
        _crypto.Verify(x => x.GenerateKey(), Times.Once);
    }

    [Fact]
    public async Task GetCurrentKeyAsync_ForExistingUser_ReturnsSameKey()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var firstKey = await _sut.GetCurrentKeyAsync(userId);

        // Act
        var secondKey = await _sut.GetCurrentKeyAsync(userId);

        // Assert
        Assert.Equal(firstKey, secondKey);
        _crypto.Verify(x => x.GenerateKey(), Times.Once);
    }

    [Fact]
    public async Task RotateKeyAsync_GeneratesNewKey()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var oldKey = await _sut.GetCurrentKeyAsync(userId);
        var newKey = (byte[])[1, 2, 3, .. new byte[32 - 3]]; // Different key
        _crypto.Setup(x => x.GenerateKey()).Returns(newKey);

        // Act
        var (key, version) = await _sut.RotateKeyAsync(userId);

        // Assert
        Assert.Equal(newKey, key);
        Assert.Equal(2, version); // Second version
        Assert.NotEqual(oldKey, key);
    }

    [Fact]
    public async Task GetKeyByVersionAsync_ReturnsCorrectKey()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var firstKey = await _sut.GetCurrentKeyAsync(userId);
        var secondKey = (byte[])[1, 2, 3, .. new byte[32 - 3]];
        _crypto.Setup(x => x.GenerateKey()).Returns(secondKey);
        await _sut.RotateKeyAsync(userId);

        // Act
        var key1 = await _sut.GetKeyByVersionAsync(userId, 1);
        var key2 = await _sut.GetKeyByVersionAsync(userId, 2);

        // Assert
        Assert.Equal(firstKey, key1);
        Assert.Equal(secondKey, key2);
    }

    [Fact]
    public async Task GetKeyByVersionAsync_WithInvalidVersion_ThrowsKeyNotFoundException()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await _sut.GetCurrentKeyAsync(userId);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(
            () => _sut.GetKeyByVersionAsync(userId, 999));
    }

    [Fact]
    public async Task GetAllKeysAsync_ReturnsAllVersions()
    {
        // Arrange
        var userId = Guid.NewGuid();
        var firstKey = await _sut.GetCurrentKeyAsync(userId);
        var secondKey = (byte[])[1, 2, 3, .. new byte[32 - 3]];
        _crypto.Setup(x => x.GenerateKey()).Returns(secondKey);
        await _sut.RotateKeyAsync(userId);

        // Act
        var allKeys = await _sut.GetAllKeysAsync(userId);

        // Assert
        Assert.Equal(2, allKeys.Count);
        Assert.Equal(firstKey, allKeys[1]);
        Assert.Equal(secondKey, allKeys[2]);
    }

    [Fact]
    public async Task RevokeKeyAsync_RemovesKey()
    {
        // Arrange
        var userId = Guid.NewGuid();
        await _sut.GetCurrentKeyAsync(userId);
        var secondKey = (byte[])[1, 2, 3, .. new byte[32 - 3]];
        _crypto.Setup(x => x.GenerateKey()).Returns(secondKey);
        await _sut.RotateKeyAsync(userId);

        // Act
        var revoked = await _sut.RevokeKeyAsync(userId, 1);
        var allKeys = await _sut.GetAllKeysAsync(userId);

        // Assert
        Assert.True(revoked);
        Assert.Single(allKeys);
        Assert.Equal(secondKey, allKeys[2]);
    }
}
