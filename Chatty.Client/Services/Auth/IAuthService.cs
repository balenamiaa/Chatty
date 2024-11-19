using Chatty.Shared.Models.Auth;
using Chatty.Shared.Models.Users;

namespace Chatty.Client.Services;

/// <summary>
///     Service for managing authentication and authorization
/// </summary>
public interface IAuthService
{
    /// <summary>
    ///     Gets whether the user is currently authenticated
    /// </summary>
    bool IsAuthenticated { get; }

    /// <summary>
    ///     Gets the current authentication token
    /// </summary>
    string? CurrentToken { get; }

    /// <summary>
    ///     Registers a new user
    /// </summary>
    Task<UserDto> RegisterAsync(CreateUserRequest request, CancellationToken ct = default);

    /// <summary>
    ///     Logs in with username/email and password
    /// </summary>
    Task<AuthResponse> LoginAsync(AuthRequest request, CancellationToken ct = default);

    /// <summary>
    ///     Logs out the current user
    /// </summary>
    Task LogoutAsync(CancellationToken ct = default);

    /// <summary>
    ///     Refreshes the current authentication token
    /// </summary>
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);

    /// <summary>
    ///     Validates the current authentication token
    /// </summary>
    Task<bool> ValidateTokenAsync(string token, CancellationToken ct = default);
}
