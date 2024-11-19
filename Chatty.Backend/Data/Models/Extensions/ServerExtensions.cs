using Chatty.Shared.Models.Servers;

namespace Chatty.Backend.Data.Models.Extensions;

public static class ServerExtensions
{
    public static ServerDto ToDto(this Server server) => new(
        server.Id,
        server.Name,
        server.Owner.ToDto(),
        server.IconUrl,
        server.CreatedAt,
        server.UpdatedAt,
        server.Roles.Select(r => r.ToDto()).ToList(),
        server.Members.Select(m => m.ToDto()).ToList());

    public static ServerRoleDto ToDto(this ServerRole role) => new(
        role.Id,
        role.ServerId,
        role.Name,
        role.Color,
        role.IsDefault,
        role.Permissions.Select(p => p.Permission).ToHashSet(),
        role.Position,
        role.CreatedAt);
}
