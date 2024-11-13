using Chatty.Shared.Models.Auth;
using Chatty.Shared.Models.Common;

namespace Chatty.Backend.Services.Auth;

public interface IAuthService
{
    Task<Result<AuthResponse>> AuthenticateAsync(AuthRequest request, CancellationToken ct = default);
    Task<Result<AuthResponse>> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<Result<bool>> RevokeTokenAsync(string refreshToken, CancellationToken ct = default);
}