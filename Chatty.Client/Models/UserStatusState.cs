using Chatty.Shared.Models.Enums;

namespace Chatty.Client.Models;

/// <summary>
///     Wrapper for user status to allow caching
/// </summary>
public record UserStatusState(UserStatus Value);
