using Chatty.Shared.Models.Enums;

using MessagePack;

namespace Chatty.Shared.Models.Servers;

[MessagePackObject]
public record UpdateServerRoleRequest(
    [property: Key(0)] string? Name,
    [property: Key(1)] string? Color,
    [property: Key(2)] bool? IsMentionable,
    [property: Key(3)] int? Position,
    [property: Key(4)] ICollection<PermissionType>? Permissions);
