using Chatty.Shared.Models.Servers;
using Chatty.Shared.Models.Users;

namespace Chatty.Shared.Realtime.Events;

public sealed record ServerCreatedEvent(ServerDto Server);

public sealed record ServerUpdatedEvent(ServerDto Server);

public sealed record ServerDeletedEvent(Guid ServerId);

public sealed record ServerMemberJoinedEvent(
    Guid ServerId,
    ServerMemberDto Member);

public sealed record ServerMemberLeftEvent(
    Guid ServerId,
    UserDto User);

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