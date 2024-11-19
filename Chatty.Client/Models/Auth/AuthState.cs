using MessagePack;

namespace Chatty.Client.Models.Auth;

/// <summary>
///     Represents the authentication state of the client
/// </summary>
[MessagePackObject]
public sealed record AuthState
{
    /// <summary>
    ///     The JWT access token
    /// </summary>
    [Key(0)]
    public string? Token { get; init; }

    /// <summary>
    ///     When the token expires
    /// </summary>
    [Key(1)]
    public DateTime? ExpiresAt { get; init; }
}
