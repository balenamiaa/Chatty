using MessagePack;

namespace Chatty.Shared.Models.Enums;

[Flags]
public enum PermissionType : long
{
    [Key(0)] None = 0,

    // General permissions
    [Key(1)] ViewChannels = 1 << 0,
    [Key(2)] ManageChannels = 1 << 1,
    [Key(3)] ManageRoles = 1 << 2,
    [Key(4)] ManageServer = 1 << 3,
    [Key(23)] CreateInvites = 1 << 22,

    // Message permissions
    [Key(5)] SendMessages = 1 << 4,
    [Key(6)] EmbedLinks = 1 << 5,
    [Key(7)] AttachFiles = 1 << 6,
    [Key(8)] ReadMessageHistory = 1 << 7,
    [Key(9)] MentionEveryone = 1 << 8,
    [Key(21)] PinMessages = 1 << 20,

    [Key(22)] AddReactions = 1 << 21,

    // Voice permissions
    [Key(10)] Connect = 1 << 9,
    [Key(11)] Speak = 1 << 10,
    [Key(12)] Video = 1 << 11,
    [Key(13)] MuteMembers = 1 << 12,
    [Key(14)] DeafenMembers = 1 << 13,
    [Key(15)] MoveMembers = 1 << 14,

    // Member permissions
    [Key(16)] KickMembers = 1 << 15,
    [Key(17)] BanMembers = 1 << 16,
    [Key(18)] ManageNicknames = 1 << 17,
    [Key(19)] ManageMessages = 1 << 18,

    // Special permissions
    [Key(20)] Administrator = 1 << 19
}
