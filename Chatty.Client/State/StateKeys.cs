namespace Chatty.Client.State;

/// <summary>
///     Constants for state keys
/// </summary>
public static class StateKeys
{
    private const string Prefix = "state:";

    // User state
    public static string CurrentUser() => $"{Prefix}user:current";
    public static string UserSettings() => $"{Prefix}user:settings";
    public static string UserPreferences() => $"{Prefix}user:preferences";
    public static string UserDevices() => $"{Prefix}user:devices";
    public static string DeviceKeys() => $"{Prefix}user:device:keys";

    // Server state
    public static string Server(Guid serverId) => $"{Prefix}server:{serverId}";
    public static string ServerMembers(Guid serverId) => $"{Prefix}server:{serverId}:members";
    public static string ServerRoles(Guid serverId) => $"{Prefix}server:{serverId}:roles";
    public static string ServerSettings(Guid serverId) => $"{Prefix}server:{serverId}:settings";

    // Channel state
    public static string Channel(Guid channelId) => $"{Prefix}channel:{channelId}";
    public static string ChannelMembers(Guid channelId) => $"{Prefix}channel:{channelId}:members";
    public static string ChannelSettings(Guid channelId) => $"{Prefix}channel:{channelId}:settings";
    public static string ChannelTyping(Guid channelId) => $"{Prefix}channel:{channelId}:typing";

    // Message state
    public static string Message(Guid messageId) => $"{Prefix}message:{messageId}";
    public static string MessageReactions(Guid messageId) => $"{Prefix}message:{messageId}:reactions";
    public static string MessageReplies(Guid messageId) => $"{Prefix}message:{messageId}:replies";
    public static string MessageDraft(Guid channelId) => $"{Prefix}message:draft:{channelId}";

    // Connection state
    public static string ConnectionState() => $"{Prefix}connection:state";
    public static string ConnectionLatency() => $"{Prefix}connection:latency";
    public static string ConnectionErrors() => $"{Prefix}connection:errors";

    // Cache state
    public static string CacheStats() => $"{Prefix}cache:stats";
    public static string CacheKeys() => $"{Prefix}cache:keys";
    public static string CacheSize() => $"{Prefix}cache:size";
    public static string CacheVersion() => $"{Prefix}cache:version";
    public static string CacheLastUpdated() => $"{Prefix}cache:last_updated";

    // UI state
    public static string UiTheme() => $"{Prefix}ui:theme";
    public static string UiLayout() => $"{Prefix}ui:layout";
    public static string UiZoom() => $"{Prefix}ui:zoom";
    public static string UiSidebar() => $"{Prefix}ui:sidebar";
    public static string UiNotifications() => $"{Prefix}ui:notifications";

    // Offline state
    public static string OfflineQueue() => $"{Prefix}offline:queue";
    public static string OfflineMessages() => $"{Prefix}offline:messages";
    public static string OfflineSync() => $"{Prefix}offline:sync";

    // Auth keys
    public static string AuthToken() => $"{Prefix}auth:token";
    public static string RefreshToken() => $"{Prefix}auth:refresh_token";

    // Device keys
    public static string DevicePreKey() => $"{Prefix}device:pre_key";

    // Channel keys
    public static string ChannelKey(Guid channelId) => $"{Prefix}channel:{channelId}:key";
    public static string ChannelPreKey(Guid channelId) => $"{Prefix}channel:{channelId}:pre_key";
    public static string ChannelKeyVersion(Guid channelId) => $"{Prefix}channel:{channelId}:key_version";

    // Direct message keys
    public static string DirectMessageKey(Guid userId) => $"{Prefix}dm:{userId}:key";
    public static string DirectMessagePreKey(Guid userId) => $"{Prefix}dm:{userId}:pre_key";
    public static string DirectMessageKeyVersion(Guid userId) => $"{Prefix}dm:{userId}:key_version";

    // Server keys
    public static string ServerKey(Guid serverId) => $"{Prefix}server:{serverId}:key";
    public static string ServerPreKey(Guid serverId) => $"{Prefix}server:{serverId}:pre_key";
    public static string ServerKeyVersion(Guid serverId) => $"{Prefix}server:{serverId}:key_version";

    // Chat state
    public static string ChatState() => $"{Prefix}chat:state";

    // Settings keys
    public static string Settings() => $"{Prefix}settings";
    public static string Theme() => $"{Prefix}theme";
    public static string Language() => $"{Prefix}language";
    public static string Notifications() => $"{Prefix}notifications";
    public static string AudioSettings() => $"{Prefix}audio";
    public static string VideoSettings() => $"{Prefix}video";
}
