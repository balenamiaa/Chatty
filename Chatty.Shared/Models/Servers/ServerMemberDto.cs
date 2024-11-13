using Chatty.Shared.Models.Users;

namespace Chatty.Shared.Models.Servers;

public sealed record ServerMemberDto(
    Guid Id,
    Guid ServerId,
    UserDto User,
    ServerRoleDto? Role,
    string? Nickname,
    DateTime JoinedAt);