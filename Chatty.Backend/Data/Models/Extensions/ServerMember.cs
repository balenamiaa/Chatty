using Chatty.Shared.Models.Servers;

namespace Chatty.Backend.Data.Models.Extensions;

public static class ServerMemberExtensions
{
    public static ServerMemberDto ToDto(this ServerMember member) => new(
        member.Id,
        member.ServerId,
        member.User.ToDto(),
        member.Role?.ToDto(),
        member.Nickname,
        member.JoinedAt);
}
