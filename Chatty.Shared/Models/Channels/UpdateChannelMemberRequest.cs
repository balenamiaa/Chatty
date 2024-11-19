namespace Chatty.Shared.Models.Channels;

/// <summary>
///     Request to update a channel member's settings
/// </summary>
public sealed record UpdateChannelMemberRequest(
    Guid UserId,
    bool IsMuted = false,
    bool IsDeafened = false,
    string? Nickname = null);
