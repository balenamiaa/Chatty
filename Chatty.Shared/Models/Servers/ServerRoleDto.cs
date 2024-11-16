using Chatty.Shared.Models.Enums;

namespace Chatty.Shared.Models.Servers;

public sealed record ServerRoleDto(
    Guid Id,
    Guid ServerId,
    string Name,
    string? Color,
    bool IsDefault,
    IReadOnlySet<PermissionType> Permissions,
    int Position,
    DateTime CreatedAt);
