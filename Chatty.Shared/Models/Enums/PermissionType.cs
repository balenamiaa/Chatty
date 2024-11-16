namespace Chatty.Shared.Models.Enums;

[Flags]
public enum PermissionType : long
{
    None = 0,

    // General permissions
    ViewChannels = 1 << 0,
    ManageChannels = 1 << 1,
    ManageRoles = 1 << 2,
    ManageServer = 1 << 3,

    // Message permissions
    SendMessages = 1 << 4,
    EmbedLinks = 1 << 5,
    AttachFiles = 1 << 6,
    ReadMessageHistory = 1 << 7,
    MentionEveryone = 1 << 8,

    // Voice permissions
    Connect = 1 << 9,
    Speak = 1 << 10,
    Video = 1 << 11,
    MuteMembers = 1 << 12,
    DeafenMembers = 1 << 13,
    MoveMembers = 1 << 14,

    // Member permissions
    KickMembers = 1 << 15,
    BanMembers = 1 << 16,
    ManageNicknames = 1 << 17,
    ManageMessages = 1 << 18,

    // Special permissions
    Administrator = 1 << 19
}
