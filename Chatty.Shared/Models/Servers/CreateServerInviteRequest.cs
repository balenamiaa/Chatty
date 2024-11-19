namespace Chatty.Shared.Models.Servers;

/// <summary>
///     Request to create a server invite
/// </summary>
public record CreateServerInviteRequest(
    TimeSpan? ExpiresIn = null,
    int? MaxUses = null);
