using Chatty.Shared.Models.Messages;
using Chatty.Shared.Models.Servers;
using Chatty.Shared.Models.Users;

namespace Chatty.Shared.Realtime.Events;

public sealed record ServerCreatedEvent(ServerDto Server);

public sealed record ServerUpdatedEvent(ServerDto Server);

public sealed record ServerDeletedEvent(Guid ServerId);

public sealed record ServerMemberJoinedEvent(
    Guid ServerId,
    ServerMemberDto Member);

public sealed record ServerMemberRemovedEvent(
    Guid ServerId,
    UserDto Member);

public sealed record ServerMemberUpdatedEvent(
    Guid ServerId,
    ServerMemberDto Member);

public sealed record ServerRoleCreatedEvent(
    Guid ServerId,
    ServerRoleDto Role);

public sealed record ServerRoleUpdatedEvent(
    Guid ServerId,
    ServerRoleDto Role);

public sealed record ServerRoleDeletedEvent(
    Guid ServerId,
    Guid RoleId);

public sealed record MessageUpdatedEvent(Guid ChannelId, MessageDto Message);

public sealed record DirectMessageUpdatedEvent(DirectMessageDto Message);
