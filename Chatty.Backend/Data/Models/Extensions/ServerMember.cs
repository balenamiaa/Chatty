using Chatty.Shared.Models.Servers;

namespace Chatty.Backend.Data.Models.Extensions;

public static class ServerMemberExtensions
{
    public static ServerMemberDto ToDto(this ServerMember member) => new(
        Id: member.Id,
        ServerId: member.ServerId,
        User: member.User.ToDto(),
        Role: member.Role?.ToDto(),
        Nickname: member.Nickname,
        JoinedAt: member.JoinedAt);

}