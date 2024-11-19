namespace Chatty.Shared.Models.Servers;

public sealed record AddServerMemberRequest(Guid UserId, Guid? RoleId = null);
