using Chatty.Shared.Models.Enums;

namespace Chatty.Shared.Models.Servers;

public sealed record CreateServerRoleRequest(
    string Name,
    string? Color,
    int Position,
    ICollection<PermissionType> Permissions);