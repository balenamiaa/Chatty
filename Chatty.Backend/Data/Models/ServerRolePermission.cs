using Chatty.Shared.Models.Enums;

namespace Chatty.Backend.Data.Models;

public sealed class ServerRolePermission
{
    public Guid RoleId { get; set; }
    public PermissionType Permission { get; set; }

    // Navigation property
    public ServerRole Role { get; set; } = null!;
}