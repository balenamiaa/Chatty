using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Services.Auth;
using Chatty.Backend.Tests.Helpers;
using Chatty.Shared.Crypto;
using Chatty.Shared.Models.Auth;
using Chatty.Shared.Models.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Chatty.Backend.Tests.Services.Auth;

public sealed class AuthServiceTests : IDisposable
{
    private readonly Mock<ICryptoProvider> _crypto;
    private readonly ChattyDbContext _context;
    private readonly AuthService _service;
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuthServiceTests()
    {
        var configBuilder = new ConfigurationBuilder();
        configBuilder.AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = "super_secret_key_for_testing_only_do_not_use_in_production_123",
            ["Jwt:Issuer"] = "chatty",
            ["Jwt:Audience"] = "chatty-client",
            ["Jwt:TokenExpirationMinutes"] = "60"
        });
        _configuration = configBuilder.Build();

        _logger = Mock.Of<ILogger<AuthService>>();
        _crypto = new Mock<ICryptoProvider>();
        _crypto.Setup(x => x.GenerateKey())
            .Returns([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16]);

        var httpContext = new DefaultHttpContext();
        httpContext.Request.Headers["User-Agent"] = "Test Browser";
        _httpContextAccessor = new HttpContextAccessor
        {
            HttpContext = httpContext
        };

        var options = new DbContextOptionsBuilder<ChattyDbContext>()
            .UseInMemoryDatabase(databaseName: $"ChattyTest_{Guid.NewGuid()}")
            .Options;
        _context = new ChattyDbContext(options);

        _service = new AuthService(_context, _httpContextAccessor, _crypto.Object, _configuration, _logger);

        // Add test user to database
        _context.Users.Add(TestData.Users.User1);
        _context.SaveChanges(); // Use synchronous SaveChanges in constructor
    }

    [Fact]
    public async Task AuthenticateAsync_WithValidCredentials_ReturnsSuccess()
    {
        // Act
        var result = await _service.AuthenticateAsync(new AuthRequest(
            TestData.Users.User1.Email,
            TestData.Auth.DefaultPassword));

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value.AccessToken);
        Assert.NotNull(result.Value.RefreshToken);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidEmail_ReturnsFailure()
    {
        // Act
        var result = await _service.AuthenticateAsync(new AuthRequest(
            "invalid@email.com",
            "password123"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("User not found", result.Error.Message);
    }

    [Fact]
    public async Task AuthenticateAsync_WithInvalidPassword_ReturnsFailure()
    {
        // Act
        var result = await _service.AuthenticateAsync(new AuthRequest(
            TestData.Users.User1.Email,
            "wrongpassword"));

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Invalid password", result.Error.Message);
    }

    [Fact]
    public async Task RefreshTokenAsync_WithValidToken_ReturnsSuccess()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var request = new AuthRequest(
            Email: TestData.Users.User1.Email,
            Password: TestData.Auth.DefaultPassword,
            DeviceId: deviceId.ToString(),
            DeviceName: "Test Device");

        var authResult = await _service.AuthenticateAsync(request);
        Assert.True(authResult.IsSuccess, "Initial authentication failed");
        Assert.NotNull(authResult.Value.RefreshToken);

        // Act
        var result = await _service.RefreshTokenAsync(authResult.Value.RefreshToken);

        // Assert
        Assert.True(result.IsSuccess, "Token refresh failed");
        Assert.NotNull(result.Value.AccessToken);
        Assert.NotNull(result.Value.RefreshToken);
        Assert.NotEqual(authResult.Value.AccessToken, result.Value.AccessToken);
        Assert.NotEqual(authResult.Value.RefreshToken, result.Value.RefreshToken);
        Assert.Equal(TestData.Users.User1.Id, result.Value.User.Id);

        // Verify device is still associated
        var device = await _context.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == TestData.Users.User1.Id && d.DeviceId == deviceId);
        Assert.NotNull(device);
        Assert.True(device.LastActiveAt > DateTime.UtcNow.AddMinutes(-1));
    }

    [Fact]
    public async Task AuthenticateAsync_WithDevice_RegistersDeviceAndGeneratesKeys()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var request = new AuthRequest(
            Email: TestData.Users.User1.Email,
            Password: TestData.Auth.DefaultPassword,
            DeviceId: deviceId.ToString(),
            DeviceName: "Test Device");

        // Act
        var result = await _service.AuthenticateAsync(request);

        // Assert
        Assert.True(result.IsSuccess, "Authentication failed");

        var device = await _context.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == TestData.Users.User1.Id && d.DeviceId == deviceId);
        Assert.NotNull(device);
        Assert.Equal("Test Device", device.DeviceName);
        Assert.Equal(DeviceType.Web, device.DeviceType);
        Assert.Equal(new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 }, device.PublicKey);

        _crypto.Verify(x => x.GenerateKey(), Times.AtLeast(2));
    }

    [Fact]
    public async Task AuthenticateAsync_WithExistingDevice_UpdatesLastActive()
    {
        // Arrange
        var deviceId = Guid.NewGuid();
        var oldLastActive = DateTime.UtcNow.AddDays(-1);
        await AddTestDevice(TestData.Users.User1.Id, deviceId, oldLastActive);

        var request = new AuthRequest(
            Email: TestData.Users.User1.Email,
            Password: TestData.Auth.DefaultPassword,
            DeviceId: deviceId.ToString(),
            DeviceName: "Updated Device");

        // Act
        var result = await _service.AuthenticateAsync(request);

        // Assert
        Assert.True(result.IsSuccess);
        var device = await _context.UserDevices
            .FirstOrDefaultAsync(d => d.UserId == TestData.Users.User1.Id && d.DeviceId == deviceId);
        Assert.True(device!.LastActiveAt > oldLastActive);
    }

    private async Task AddTestDevice(Guid userId, Guid deviceId, DateTime lastActive)
    {
        _context.UserDevices.Add(new UserDevice
        {
            UserId = userId,
            DeviceId = deviceId,
            LastActiveAt = lastActive,
            DeviceType = DeviceType.Web,
            DeviceName = "Test Device",
            PublicKey = new byte[32]
        });
        await _context.SaveChangesAsync();
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}