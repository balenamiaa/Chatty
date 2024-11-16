using Chatty.Shared.Models.Users;

namespace Chatty.Shared.Models.Auth;

public sealed record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    UserDto User);
