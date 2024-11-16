using Chatty.Shared.Models.Channels;
using Chatty.Shared.Models.Users;

namespace Chatty.Shared.Realtime.Events;

public sealed record ChannelCreatedEvent(
    Guid ServerId,
    ChannelDto Channel);

public sealed record ChannelUpdatedEvent(
    Guid ServerId,
    ChannelDto Channel);

public sealed record ChannelDeletedEvent(
    Guid ServerId,
    Guid ChannelId);

public sealed record ChannelMemberJoinedEvent(
    Guid ServerId,
    Guid ChannelId,
    UserDto User);

public sealed record ChannelMemberLeftEvent(
    Guid ServerId,
    Guid ChannelId,
    UserDto User);
