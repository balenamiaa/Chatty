using MessagePack;

namespace Chatty.Shared.Models.Servers;

[MessagePackObject]
public record UpdateServerMemberRequest(
    [property: Key(0)] string? Nickname,
    [property: Key(1)] Guid? RoleId,
    [property: Key(2)] bool? IsMuted,
    [property: Key(3)] bool? IsDeafened
);
