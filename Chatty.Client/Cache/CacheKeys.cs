namespace Chatty.Client.Cache;

/// <summary>
///     Helper class for generating cache keys
/// </summary>
public static class CacheKeys
{
    private const string Prefix = "chatty:";

    // Server keys
    public static string Server(Guid serverId) => $"{Prefix}server:{serverId}";
    public static string ServerList() => $"{Prefix}servers";
    public static string ServerMembers(Guid serverId) => $"{Prefix}server:{serverId}:members";
    public static string ServerRoles(Guid serverId) => $"{Prefix}server:{serverId}:roles";
    public static string ServerInvites(Guid serverId) => $"{Prefix}server:{serverId}:invites";
    public static string ServerCategories(Guid serverId) => $"{Prefix}server:{serverId}:categories";
    public static string ServerChannels(Guid serverId) => $"{Prefix}server:{serverId}:channels";
    public static string JoinedServers() => $"{Prefix}servers:joined";

    // Channel keys
    public static string Channel(Guid channelId) => $"{Prefix}channel:{channelId}";
    public static string ChannelList(Guid serverId) => $"{Prefix}server:{serverId}:channels";
    public static string ChannelMembers(Guid channelId) => $"{Prefix}channel:{channelId}:members";
    public static string ChannelMessages(Guid channelId) => $"{Prefix}channel:{channelId}:messages";
    public static string ChannelKey(Guid channelId) => $"{Prefix}channel:{channelId}:key";
    public static string ChannelTypingUsers(Guid channelId) => $"{Prefix}channel:{channelId}:typing";
    public static string ChannelPermissions(Guid channelId) => $"{Prefix}channel:{channelId}:permissions";
    public static string ChannelInvites(Guid channelId) => $"{Prefix}channel:{channelId}:invites";
    public static string ChannelVoiceVideoUsers(Guid channelId) => $"{Prefix}channel:{channelId}:voice_video_users";

    // User keys
    public static string User(Guid userId) => $"{Prefix}user:{userId}";
    public static string UserMe() => $"{Prefix}user:me";
    public static string UserSettings() => $"{Prefix}user:me:settings";
    public static string UserFriends() => $"{Prefix}user:me:friends";
    public static string UserDevices() => $"{Prefix}user:me:devices";
    public static string UserBlockedUsers() => $"{Prefix}user:me:blocked";
    public static string UserPresence(Guid userId) => $"{Prefix}user:{userId}:presence";
    public static string UserStatus(Guid userId) => $"{Prefix}user:{userId}:status";
    public static string UserActivity(Guid userId) => $"{Prefix}user:{userId}:activity";
    public static string UserStreaming(Guid userId) => $"{Prefix}user:{userId}:streaming";

    // Message keys
    public static string Message(Guid messageId) => $"{Prefix}message:{messageId}";
    public static string DirectMessage(Guid messageId) => $"{Prefix}direct_message:{messageId}";
    public static string DirectMessages(Guid userId1, Guid userId2) => $"{Prefix}direct_messages:{userId1}:{userId2}";
    public static string MessageReactions(Guid messageId) => $"{Prefix}message:{messageId}:reactions";
    public static string DirectMessageReactions(Guid messageId) => $"{Prefix}direct_message:{messageId}:reactions";

    public static string MessageReactionsByType(Guid messageId, string type) =>
        $"{Prefix}message:{messageId}:reactions:{type}";

    public static string UserReactions(Guid userId) => $"{Prefix}user:{userId}:reactions";
    public static string MessageReplies(Guid messageId) => $"{Prefix}message:{messageId}:replies";
    public static string MessageReplyCount(Guid messageId) => $"{Prefix}message:{messageId}:reply-count";
    public static string UserMentions(Guid userId) => $"{Prefix}user:{userId}:mentions";
    public static string MessageParent(Guid messageId) => $"{Prefix}message:{messageId}:parent";
    public static string MessageChildren(Guid messageId) => $"{Prefix}message:{messageId}:children";
    public static string MessageThread(Guid messageId) => $"{Prefix}message:{messageId}:thread";

    // Call keys
    public static string Call(Guid callId) => $"{Prefix}call:{callId}";
    public static string CallParticipants(Guid callId) => $"{Prefix}call:{callId}:participants";

    // File keys
    public static string File(Guid fileId) => $"{Prefix}file:{fileId}";
    public static string FileContent(Guid fileId) => $"{Prefix}file:{fileId}:content";
    public static string FileKey(Guid fileId) => $"{Prefix}file:{fileId}:key";

    // Device keys
    public static string DeviceId() => $"{Prefix}device:id";
    public static string DeviceName() => $"{Prefix}device:name";
    public static string DeviceKeys() => $"{Prefix}device:keys";
    public static string DevicePreKey() => $"{Prefix}device:prekey";
}
