namespace Chatty.Shared.Models.Channels;

/// <summary>
///     Represents a channel invite
/// </summary>
public record ChannelInviteDto(
    Guid ChannelId,
    string InviteCode,
    Guid CreatedBy,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    int? MaxUses,
    int UsedCount,
    bool IsRevoked);
