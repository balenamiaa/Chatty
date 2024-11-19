using MessagePack;

namespace Chatty.Shared.Models.Users;

[MessagePackObject]
public record UserSettingsDto(
    [property: Key(0)] Guid UserId,
    [property: Key(1)] bool EmailNotifications,
    [property: Key(2)] bool PushNotifications,
    [property: Key(3)] bool SoundEnabled,
    [property: Key(4)] string Theme,
    [property: Key(5)] string Language,
    [property: Key(6)] bool CompactView,
    [property: Key(7)] bool AutoPlayGifs,
    [property: Key(8)] bool AutoPlayVideos,
    [property: Key(9)] bool ShowOfflineUsers,
    [property: Key(10)] DateTime UpdatedAt
);
