namespace Chatty.Shared.Models.Channels;

/// <summary>
///     Request to update channel permissions
/// </summary>
public record UpdateChannelPermissionsRequest(
    bool? CanRead = null,
    bool? CanWrite = null,
    bool? CanManage = null,
    bool? CanInvite = null,
    bool? CanKick = null,
    bool? CanBan = null,
    bool? CanPin = null,
    bool? CanMention = null,
    bool? CanUpload = null,
    bool? CanEmbed = null,
    bool? CanUseVoice = null,
    bool? CanStream = null,
    bool? CanManagePermissions = null);
