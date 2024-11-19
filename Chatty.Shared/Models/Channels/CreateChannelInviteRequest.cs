namespace Chatty.Shared.Models.Channels;

/// <summary>
///     Request to create a channel invite
/// </summary>
public record CreateChannelInviteRequest(
    TimeSpan? ExpiresIn = null,
    int? MaxUses = null);
