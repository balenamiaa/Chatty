using Chatty.Shared.Models.Enums;

namespace Chatty.Client.Http;

/// <summary>
///     Strongly typed API endpoints for the Chatty API
/// </summary>
public static class ApiEndpoints
{
    private const string ApiPrefix = "api/v1/";

    public static class Auth
    {
        private const string Prefix = $"{ApiPrefix}auth/";

        public const string Register = $"{Prefix}register";
        public const string Login = $"{Prefix}login";
        public const string Logout = $"{Prefix}logout";
        public const string Refresh = $"{Prefix}refresh";
        public const string Validate = $"{Prefix}validate";
    }

    public static class Users
    {
        private const string Prefix = $"{ApiPrefix}users/";

        public const string Me = $"{Prefix}me";
        public const string Friends = $"{Me}/friends";
        public const string BlockedUsers = $"{Me}/blocked";
        public static string Settings = $"{Me}/settings";
        public static string Devices = $"{Me}/devices";
        public static string Password = $"{Me}/password";
        public static string PasswordReset = $"{Prefix}password/reset";
        public static string PasswordResetConfirm = $"{PasswordReset}/confirm";
        public static string Profile(Guid userId) => $"{Prefix}{userId}";
        public static string Device(Guid deviceId) => $"{Devices}/{deviceId}";
        public static string FriendRequest(Guid userId) => $"{Friends}/requests/{userId}";
        public static string AcceptFriendRequest(Guid userId) => $"{FriendRequest(userId)}/accept";
        public static string RejectFriendRequest(Guid userId) => $"{FriendRequest(userId)}/reject";
        public static string RemoveFriend(Guid userId) => $"{Friends}/{userId}";
        public static string BlockUser(Guid userId) => $"{BlockedUsers}/{userId}";
        public static string UnblockUser(Guid userId) => $"{BlockUser(userId)}";
        public static string DirectMessages(Guid userId) => $"{Prefix}{userId}/messages";
        public static string DirectMessage(Guid messageId) => $"{DirectMessages}/{messageId}";
    }

    public static class Servers
    {
        private const string Prefix = $"{ApiPrefix}servers/";

        public const string List = Prefix;
        public static string Server(Guid serverId) => $"{Prefix}{serverId}";
        public static string Members(Guid serverId) => $"{Server(serverId)}/members";
        public static string Member(Guid serverId, Guid userId) => $"{Members(serverId)}/{userId}";
        public static string MemberKick(Guid serverId, Guid userId) => $"{Member(serverId, userId)}/kick";
        public static string Roles(Guid serverId) => $"{Server(serverId)}/roles";
        public static string Role(Guid serverId, Guid roleId) => $"{Roles(serverId)}/{roleId}";
        public static string Invites(Guid serverId) => $"{Server(serverId)}/invites";
        public static string Invite(Guid serverId, Guid inviteId) => $"{Invites(serverId)}/{inviteId}";
        public static string Join(string inviteCode) => $"{Prefix}join/{inviteCode}";
        public static string Categories(Guid serverId) => $"{Server(serverId)}/categories";
        public static string Category(Guid serverId, Guid categoryId) => $"{Categories(serverId)}/{categoryId}";
    }

    public static class Channels
    {
        private const string Prefix = $"{ApiPrefix}channels/";

        public const string List = Prefix;
        public static string Channel(Guid channelId) => $"{Prefix}{channelId}";
        public static string Messages(Guid channelId) => $"{Channel(channelId)}/messages";
        public static string Message(Guid channelId, Guid messageId) => $"{Messages(channelId)}/{messageId}";
        public static string Members(Guid channelId) => $"{Channel(channelId)}/members";
        public static string Member(Guid channelId, Guid userId) => $"{Members(channelId)}/{userId}";
        public static string MemberRole(Guid channelId, Guid userId) => $"{Member(channelId, userId)}/role";
        public static string MemberKick(Guid channelId, Guid userId) => $"{Member(channelId, userId)}/kick";
        public static string Permissions(Guid channelId) => $"{Channel(channelId)}/permissions";
        public static string Permission(Guid channelId, Guid roleId) => $"{Permissions(channelId)}/{roleId}";
        public static string Invites(Guid channelId) => $"{Channel(channelId)}/invites";
        public static string Invite(Guid channelId, Guid inviteId) => $"{Invites(channelId)}/{inviteId}";
        public static string Join(string inviteCode) => $"{Prefix}join/{inviteCode}";
        public static string Pins(Guid channelId) => $"{Channel(channelId)}/pins";
        public static string Pin(Guid channelId, Guid messageId) => $"{Pins(channelId)}/{messageId}";
        public static string Read(Guid channelId) => $"{Channel(channelId)}/read";
        public static string ServerChannels(Guid serverId) => $"{ApiPrefix}servers/{serverId}/channels";
        public static string ServerCategories(Guid serverId) => $"{ApiPrefix}servers/{serverId}/categories";
        public static string Category(Guid categoryId) => $"{ApiPrefix}categories/{categoryId}";
        public static string ChannelMembers(Guid channelId) => $"{Channel(channelId)}/members";
        public static string ChannelMember(Guid channelId, Guid userId) => $"{ChannelMembers(channelId)}/{userId}";
        public static string ChannelPermissions(Guid channelId) => $"{Channel(channelId)}/permissions";
        public static string ChannelInvites(Guid channelId) => $"{Channel(channelId)}/invites";

        public static string ChannelInvite(Guid channelId, string inviteCode) =>
            $"{ChannelInvites(channelId)}/{inviteCode}";
    }

    public static class Presence
    {
        private const string Prefix = $"{ApiPrefix}presence/";

        public const string Status = $"{Prefix}status";
        public const string UserStatusBatch = $"{Prefix}users/batch";
        public static string UserStatus(Guid userId) => $"{Prefix}users/{userId}";
    }

    public static class Messages
    {
        private const string Prefix = $"{ApiPrefix}messages/";

        public static string ChannelMessages(Guid channelId, int limit, Guid? before = null) =>
            $"{Prefix}channel/{channelId}?limit={limit}{(before.HasValue ? $"&before={before}" : "")}";

        public static string DirectMessages(Guid otherUserId, int limit, Guid? before = null) =>
            $"{Prefix}direct/{otherUserId}?limit={limit}{(before.HasValue ? $"&before={before}" : "")}";

        public static string Message(Guid messageId) => $"{Prefix}{messageId}";

        public static string MessageReactions(Guid messageId) => $"{Message(messageId)}/reactions";

        public static string MessageReactionsByType(Guid messageId, ReactionType type) =>
            $"{MessageReactions(messageId)}/{type}";

        public static string MessageReplies(Guid messageId, int limit, DateTime? before = null) =>
            $"{Message(messageId)}/replies?limit={limit}{(before.HasValue ? $"&before={before:O}" : "")}";

        public static string MessageReplyCount(Guid messageId) => $"{Message(messageId)}/replies/count";

        public static string UserReaction(Guid messageId, Guid userId) =>
            $"{MessageReactions(messageId)}/users/{userId}";

        public static string ParentMessage(Guid messageId) =>
            $"{Message(messageId)}/parent";

        public static string Mentions(Guid userId, int limit, DateTime? before = null, DateTime? after = null) =>
            $"{Prefix}mentions/{userId}?limit={limit}{string.Join("&", new[]
            {
                before.HasValue ? $"before={before}" : null,
                after.HasValue ? $"after={after}" : null
            }.Where(x => x != null))}";
    }
}
