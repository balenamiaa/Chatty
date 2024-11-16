using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Shared.Models.Auth;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Collections.Concurrent;
using ICryptoProvider = Chatty.Shared.Crypto.ICryptoProvider;

namespace Chatty.Backend.Services.Auth;

public sealed class AuthService(
    ChattyDbContext context,
    IHttpContextAccessor httpContextAccessor,
    ICryptoProvider crypto,
    IConfiguration configuration,
    ILogger logger) : IAuthService
{
    private static DeviceType DetermineDeviceType(string? userAgent)
    {
        if (string.IsNullOrEmpty(userAgent))
            return DeviceType.Web;

        userAgent = userAgent.ToLower();

        if (userAgent.Contains("android"))
            return DeviceType.Android;
        if (userAgent.Contains("ios"))
            return DeviceType.iOS;
        return userAgent.Contains("desktop") ? DeviceType.Desktop : DeviceType.Web;
    }

    public async Task<Result<AuthResponse>> AuthenticateAsync(AuthRequest request, CancellationToken ct = default)
    {
        // Validate user credentials first
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        if (user is null)
            return Result<AuthResponse>.Failure(Error.NotFound("User not found"));

        if (!VerifyPasswordHash(request.Password, user.PasswordHash))
            return Result<AuthResponse>.Failure(Error.Unauthorized("Invalid password"));

        // Handle device registration if provided
        if (request.DeviceId is not null)
        {
            var deviceResult = await HandleDeviceRegistrationAsync(
                user.Id,
                request.DeviceId,
                request.DeviceName,
                ct);

            if (deviceResult.IsFailure)
                return Result<AuthResponse>.Failure(deviceResult.Error);
        }

        // Generate tokens after all changes are saved
        var token = GenerateAccessToken(user);
        var refreshToken = GenerateRefreshToken(user);

        return Result<AuthResponse>.Success(new AuthResponse(
            AccessToken: token,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddHours(1),
            User: user.ToDto()
        ));
    }

    private async Task<Result<bool>> HandleDeviceRegistrationAsync(
        Guid userId,
        string deviceId,
        string? deviceName,
        CancellationToken ct)
    {
        try
        {
            var parsedDeviceId = Guid.Parse(deviceId);
            var device = await context.UserDevices
                .FirstOrDefaultAsync(d =>
                    d.UserId == userId &&
                    d.DeviceId == parsedDeviceId, ct);

            var deviceType = DetermineDeviceType(
                httpContextAccessor.HttpContext?.Request.Headers.UserAgent);

            if (device is not null)
            {
                // Update existing device
                device.LastActiveAt = DateTime.UtcNow;
                device.DeviceName = deviceName ?? device.DeviceName;
                device.DeviceType = deviceType;
            }
            else
            {
                // Generate keys for E2E encryption
                var publicKey = crypto.GenerateKey();
                var preKeyPublic = crypto.GenerateKey();

                // Create new device with E2E encryption keys
                var preKey = new PreKey
                {
                    UserId = userId,
                    DeviceId = parsedDeviceId,
                    PreKeyId = 1, // Initial pre-key
                    PreKeyPublic = preKeyPublic
                };

                context.UserDevices.Add(new UserDevice
                {
                    UserId = userId,
                    DeviceId = parsedDeviceId,
                    DeviceName = deviceName ?? "Unknown Device",
                    DeviceType = deviceType,
                    PublicKey = publicKey,
                    LastActiveAt = DateTime.UtcNow
                });

                context.PreKeys.Add(preKey);
            }

            await context.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }
        catch (FormatException)
        {
            return Result<bool>.Failure(Error.InvalidInput("Invalid device ID format"));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to handle device registration for user {UserId}", userId);
            return Result<bool>.Failure(Error.Internal("Failed to register device"));
        }
    }

    public async Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        try
        {
            // Validate refresh token
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                ValidateLifetime = false, // Don't validate lifetime for refresh tokens
                ClockSkew = TimeSpan.Zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            ClaimsPrincipal principal;
            try
            {
                principal = tokenHandler.ValidateToken(refreshToken, tokenValidationParameters, out var validatedToken);

                // Check if token has expired (manual check since ValidateLifetime is false)
                var jwtToken = (JwtSecurityToken)validatedToken;
                var expirationTime = jwtToken.ValidTo.ToUniversalTime();
                if (expirationTime < DateTime.UtcNow)
                    return Result<AuthResponse>.Failure(Error.Unauthorized("Refresh token has expired"));

                // Check if token type is refresh
                if (principal.FindFirst("token_type")?.Value != "refresh")
                    return Result<AuthResponse>.Failure(Error.Unauthorized("Invalid token type"));
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to validate refresh token");
                return Result<AuthResponse>.Failure(Error.Unauthorized("Invalid refresh token"));
            }

            var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null)
                return Result<AuthResponse>.Failure(Error.NotFound("User not found"));

            // Check if token has been revoked
            if (IsTokenRevoked(refreshToken))
                return Result<AuthResponse>.Failure(Error.Unauthorized("Token has been revoked"));

            // Generate new tokens
            var accessToken = GenerateAccessToken(user);
            var newRefreshToken = GenerateRefreshToken(user);

            return Result<AuthResponse>.Success(new AuthResponse(
                AccessToken: accessToken,
                RefreshToken: newRefreshToken,
                ExpiresAt: DateTime.UtcNow.AddHours(1),
                User: user.ToDto()
            ));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to refresh token");
            return Result<AuthResponse>.Failure(Error.Internal("Failed to refresh token"));
        }
    }

    public async Task<Result<bool>> RevokeTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        try
        {
            // Validate refresh token
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!)),
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                ValidateLifetime = false, // Don't validate lifetime for revocation
                ClockSkew = TimeSpan.Zero
            };

            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(refreshToken, tokenValidationParameters, out _);

            var userId = Guid.Parse(principal.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            // For single backend, we can use a static ConcurrentDictionary
            RevokedTokens.TryAdd(refreshToken, DateTime.UtcNow);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to revoke refresh token");
            return Result<bool>.Failure(Error.Internal("Failed to revoke token"));
        }
    }

    private string GenerateRefreshToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ??
                                        throw new InvalidOperationException("JWT key not configured"));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim("token_type", "refresh"),
                new Claim("jti", Guid.NewGuid().ToString()) // Add unique identifier
            }),
            Expires = DateTime.UtcNow.AddDays(7), // Refresh tokens last longer
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            NotBefore = DateTime.UtcNow // Add NotBefore to ensure uniqueness
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateAccessToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ??
                                         throw new InvalidOperationException("JWT key not configured"));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim("token_type", "access"),
                new Claim("jti", Guid.NewGuid().ToString()) // Add unique identifier
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            NotBefore = DateTime.UtcNow // Add NotBefore to ensure uniqueness
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static bool VerifyPasswordHash(string password, string storedHash)
    {
        try
        {
            // BCrypt handles salt internally
            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
        catch
        {
            // If hash is invalid format, return false
            return false;
        }
    }

    // Add static token revocation store
    private static readonly ConcurrentDictionary<string, DateTime> RevokedTokens = new();

    // Add method to validate token is not revoked
    private static bool IsTokenRevoked(string token)
    {
        return RevokedTokens.TryGetValue(token, out var revokedAt);
    }

    private async Task<Result<bool>> VerifyDeviceAsync(
        Guid userId,
        string deviceId,
        CancellationToken ct)
    {
        if (string.IsNullOrEmpty(deviceId))
            return Result<bool>.Success(true); // No device to verify

        try
        {
            var parsedDeviceId = Guid.Parse(deviceId);
            var device = await context.UserDevices
                .FirstOrDefaultAsync(d =>
                    d.UserId == userId &&
                    d.DeviceId == parsedDeviceId, ct);

            // Allow new devices - they will be registered during authentication
            if (device is null)
                return Result<bool>.Success(true);

            // TODO: Implement device verification
            // For now, just verify device exists
            return Result<bool>.Success(true);
        }
        catch (FormatException)
        {
            return Result<bool>.Failure(Error.InvalidInput("Invalid device ID format"));
        }
    }
}