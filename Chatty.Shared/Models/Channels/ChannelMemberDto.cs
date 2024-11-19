using Chatty.Shared.Models.Users;

namespace Chatty.Shared.Models.Channels;

/// <summary>
///     Represents a member of a channel
/// </summary>
public record ChannelMemberDto(
    Guid Id,
    Guid ChannelId,
    UserDto User,
    DateTime JoinedAt);
