using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Infrastructure.Configuration;
using Chatty.Backend.Security.Hashing;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Users;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Chatty.Backend.Services.Users;

public sealed class UserService(
    ChattyDbContext context,
    ILogger<UserService> logger,
    IOptions<SecuritySettings> securitySettings)
    : IUserService
{
    private readonly SecuritySettings _securitySettings = securitySettings.Value;

    public async Task<Result<UserDto>> CreateAsync(
        CreateUserRequest request,
        CancellationToken ct = default)
    {
        // Check if email is already taken
        if (await context.Users.AnyAsync(u => u.Email == request.Email, ct))
        {
            return Result<UserDto>.Failure(Error.Conflict("Email already taken"));
        }

        // Check if username is already taken
        if (await context.Users.AnyAsync(u => u.Username == request.Username, ct))
        {
            return Result<UserDto>.Failure(Error.Conflict("Username already taken"));
        }

        try
        {
            var user = new User
            {
                Username = request.Username,
                Email = request.Email,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FirstName = request.FirstName,
                LastName = request.LastName,
                Locale = request.Locale ?? "en-US"
            };

            context.Users.Add(user);
            await context.SaveChangesAsync(ct);

            return Result<UserDto>.Success(user.ToDto());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create user with email {Email}", request.Email);
            return Result<UserDto>.Failure(Error.Internal("Failed to create user"));
        }
    }

    public async Task<Result<UserDto>> UpdateAsync(
        Guid userId,
        UpdateUserRequest request,
        CancellationToken ct = default)
    {
        var user = await context.Users.FindAsync([userId], ct);
        if (user is null)
        {
            return Result<UserDto>.Failure(Error.NotFound("User not found"));
        }

        try
        {
            // Check username uniqueness if changed
            if (request.Username is not null && request.Username != user.Username)
            {
                if (await context.Users.AnyAsync(u => u.Username == request.Username, ct))
                {
                    return Result<UserDto>.Failure(Error.Conflict("Username already taken"));
                }

                user.Username = request.Username;
            }

            // Update other fields
            if (request.FirstName is not null)
            {
                user.FirstName = request.FirstName;
            }

            if (request.LastName is not null)
            {
                user.LastName = request.LastName;
            }

            if (request.ProfilePictureUrl is not null)
            {
                user.ProfilePictureUrl = request.ProfilePictureUrl;
            }

            if (request.StatusMessage is not null)
            {
                user.StatusMessage = request.StatusMessage;
            }

            if (request.Locale is not null)
            {
                user.Locale = request.Locale;
            }

            user.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);

            return Result<UserDto>.Success(user.ToDto());
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update user {UserId}", userId);
            return Result<UserDto>.Failure(Error.Internal("Failed to update user"));
        }
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var user = await context.Users.FindAsync([userId], ct);
        if (user is null)
        {
            return Result<bool>.Success(true); // Already deleted
        }

        try
        {
            context.Users.Remove(user);
            await context.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to delete user {UserId}", userId);
            return Result<bool>.Failure(Error.Internal("Failed to delete user"));
        }
    }

    public async Task<Result<UserDto>> GetByIdAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var user = await context.Users.FindAsync([userId], ct);
        if (user is null)
        {
            return Result<UserDto>.Failure(Error.NotFound("User not found"));
        }

        return Result<UserDto>.Success(user.ToDto());
    }

    public async Task<Result<UserDto>> GetByEmailAsync(
        string email,
        CancellationToken ct = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
        {
            return Result<UserDto>.Failure(Error.NotFound("User not found"));
        }

        return Result<UserDto>.Success(user.ToDto());
    }

    public async Task<Result<UserDto>> GetByUsernameAsync(
        string username,
        CancellationToken ct = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Username == username, ct);

        if (user is null)
        {
            return Result<UserDto>.Failure(Error.NotFound("User not found"));
        }

        return Result<UserDto>.Success(user.ToDto());
    }

    public async Task<Result<bool>> ChangePasswordAsync(
        Guid userId,
        string currentPassword,
        string newPassword,
        CancellationToken ct = default)
    {
        var options = new HashingOptions
        {
            Iterations = _securitySettings.PasswordHashingIterations
        };

        var user = await context.Users.FindAsync([userId], ct);
        if (user is null)
        {
            return Result<bool>.Failure(Error.NotFound("User not found"));
        }

        // Verify current password
        if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
        {
            return Result<bool>.Failure(Error.Unauthorized("Invalid current password"));
        }

        try
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to change password for user {UserId}", userId);
            return Result<bool>.Failure(Error.Internal("Failed to change password"));
        }
    }

    public async Task<Result<bool>> RequestPasswordResetAsync(
        string email,
        CancellationToken ct = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
        {
            return Result<bool>.Success(true); // Don't reveal if email exists
        }

        try
        {
            // TODO: Generate reset token and send email
            // For now, just return success
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to request password reset for user {Email}", email);
            return Result<bool>.Failure(Error.Internal("Failed to request password reset"));
        }
    }

    public async Task<Result<bool>> ResetPasswordAsync(
        string email,
        string token,
        string newPassword,
        CancellationToken ct = default)
    {
        var user = await context.Users
            .FirstOrDefaultAsync(u => u.Email == email, ct);

        if (user is null)
        {
            return Result<bool>.Failure(Error.NotFound("User not found"));
        }

        try
        {
            // TODO: Verify reset token
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            user.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to reset password for user {Email}", email);
            return Result<bool>.Failure(Error.Internal("Failed to reset password"));
        }
    }

    private string HashPassword(string password) =>
        BCrypt.Net.BCrypt.HashPassword(
            password,
            _securitySettings.PasswordHashingIterations);

    private bool ValidatePassword(string password)
    {
        if (password.Length < _securitySettings.MinPasswordLength)
        {
            return false;
        }

        if (_securitySettings.RequireUppercase && !password.Any(char.IsUpper))
        {
            return false;
        }

        if (_securitySettings.RequireLowercase && !password.Any(char.IsLower))
        {
            return false;
        }

        if (_securitySettings.RequireDigit && !password.Any(char.IsDigit))
        {
            return false;
        }

        if (_securitySettings.RequireSpecialChar && !password.Any(c => !char.IsLetterOrDigit(c)))
        {
            return false;
        }

        return true;
    }
}
