using MessagePack;

namespace Chatty.Shared.Models.Users;

[MessagePackObject]
public record UpdateSettingsRequest(
    [property: Key(0)] bool? EmailNotifications,
    [property: Key(1)] bool? PushNotifications,
    [property: Key(2)] bool? SoundEnabled,
    [property: Key(3)] string? Theme,
    [property: Key(4)] string? Language,
    [property: Key(5)] bool? CompactView,
    [property: Key(6)] bool? AutoPlayGifs,
    [property: Key(7)] bool? AutoPlayVideos,
    [property: Key(8)] bool? ShowOfflineUsers
);
