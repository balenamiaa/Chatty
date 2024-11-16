using Chatty.Shared.Models.Users;

namespace Chatty.Shared.Models.Servers;

public sealed record ServerDto(
    Guid Id,
    string Name,
    UserDto Owner,
    string? IconUrl,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    IReadOnlyList<ServerRoleDto> Roles,
    IReadOnlyList<ServerMemberDto> Members);
