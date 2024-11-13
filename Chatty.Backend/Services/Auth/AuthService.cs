using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Shared.Models.Auth;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Users;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace Chatty.Backend.Services.Auth;

public sealed class AuthService(ChattyDbContext context, IConfiguration configuration) : IAuthService
{
    public async Task<Result<AuthResponse>> AuthenticateAsync(AuthRequest request, CancellationToken ct = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email, ct);

        if (user is null)
            return Result<AuthResponse>.Failure(Error.NotFound("User not found"));

        if (!VerifyPasswordHash(request.Password, user.PasswordHash))
            return Result<AuthResponse>.Failure(Error.Unauthorized("Invalid password"));

        var token = GenerateJwtToken(user);
        var refreshToken = GenerateRefreshToken();

        // Update user's device if provided
        if (request.DeviceId is not null)
        {
            var device = await context.UserDevices
                .FirstOrDefaultAsync(d => d.UserId == user.Id && d.DeviceId == Guid.Parse(request.DeviceId), ct);

            if (device is not null)
            {
                device.LastActiveAt = DateTime.UtcNow;
                device.DeviceName = request.DeviceName;
            }
            else
            {
                // Create new device
                context.UserDevices.Add(new UserDevice
                {
                    UserId = user.Id,
                    DeviceId = Guid.Parse(request.DeviceId),
                    DeviceName = request.DeviceName,
                    DeviceType = DeviceType.Web, // TODO: Determine device type
                    PublicKey = [] // TODO: Handle E2E encryption keys
                });
            }
        }

        await context.SaveChangesAsync(ct);

        return Result<AuthResponse>.Success(new AuthResponse(
            AccessToken: token,
            RefreshToken: refreshToken,
            ExpiresAt: DateTime.UtcNow.AddHours(1),
            User: user.ToDto()
        ));
    }

    public Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool>> RevokeTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    private string GenerateJwtToken(User user)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? throw new InvalidOperationException("JWT key not configured"));

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.Username)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = configuration["Jwt:Issuer"],
            Audience = configuration["Jwt:Audience"],
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature)
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private static string GenerateRefreshToken()
    {
        var randomNumber = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomNumber);
        return Convert.ToBase64String(randomNumber);
    }

    private static bool VerifyPasswordHash(string password, string storedHash)
    {
        // TODO: Implement proper password verification
        return BCrypt.Net.BCrypt.Verify(password, storedHash);
    }
}