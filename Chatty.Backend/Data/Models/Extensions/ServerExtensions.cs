using Chatty.Shared.Models.Servers;

namespace Chatty.Backend.Data.Models.Extensions;

public static class ServerExtensions
{
    public static ServerDto ToDto(this Server server) => new(
        Id: server.Id,
        Name: server.Name,
        Owner: server.Owner.ToDto(),
        IconUrl: server.IconUrl,
        CreatedAt: server.CreatedAt,
        UpdatedAt: server.UpdatedAt,
        Roles: server.Roles.Select(r => r.ToDto()).ToList(),
        Members: server.Members.Select(m => m.ToDto()).ToList());

    public static ServerRoleDto ToDto(this ServerRole role) => new(
        Id: role.Id,
        ServerId: role.ServerId,
        Name: role.Name,
        Color: role.Color,
        IsDefault: role.IsDefault,
        Permissions: role.Permissions.Select(p => p.Permission).ToHashSet(),
        Position: role.Position,
        CreatedAt: role.CreatedAt);
}