namespace Chatty.Shared.Models.Channels;

/// <summary>
///     Represents channel permissions
/// </summary>
public record ChannelPermissionsDto(
    Guid ChannelId,
    bool CanRead,
    bool CanWrite,
    bool CanManage,
    bool CanInvite,
    bool CanKick,
    bool CanBan,
    bool CanPin,
    bool CanMention,
    bool CanUpload,
    bool CanEmbed,
    bool CanUseVoice,
    bool CanStream,
    bool CanManagePermissions);
