using Chatty.Shared.Models.Channels;

namespace Chatty.Client.Services;

/// <summary>
///     Service for managing channels
/// </summary>
public interface IChannelService
{
    /// <summary>
    ///     Gets a channel by ID
    /// </summary>
    Task<ChannelDto> GetAsync(Guid channelId, CancellationToken ct = default);

    /// <summary>
    ///     Gets all channels in a server
    /// </summary>
    Task<IReadOnlyList<ChannelDto>> GetForServerAsync(
        Guid serverId,
        CancellationToken ct = default);

    /// <summary>
    ///     Creates a new channel in a server
    /// </summary>
    Task<ChannelDto> CreateAsync(
        Guid serverId,
        CreateChannelRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Updates an existing channel
    /// </summary>
    Task<ChannelDto> UpdateAsync(
        Guid channelId,
        UpdateChannelRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Deletes a channel
    /// </summary>
    Task DeleteAsync(Guid channelId, CancellationToken ct = default);

    /// <summary>
    ///     Gets the members of a channel
    /// </summary>
    Task<IReadOnlyList<ChannelMemberDto>> GetMembersAsync(
        Guid channelId,
        CancellationToken ct = default);

    /// <summary>
    ///     Adds a member to a channel
    /// </summary>
    Task AddMemberAsync(Guid channelId, Guid userId, CancellationToken ct = default);

    /// <summary>
    ///     Removes a member from a channel
    /// </summary>
    Task RemoveMemberAsync(Guid channelId, Guid userId, CancellationToken ct = default);

    /// <summary>
    ///     Gets the permissions for a channel
    /// </summary>
    Task<ChannelPermissionsDto> GetPermissionsAsync(
        Guid channelId,
        CancellationToken ct = default);

    /// <summary>
    ///     Updates the permissions for a channel
    /// </summary>
    Task<ChannelPermissionsDto> UpdatePermissionsAsync(
        Guid channelId,
        UpdateChannelPermissionsRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Creates an invite for a channel
    /// </summary>
    Task<ChannelInviteDto> CreateInviteAsync(
        Guid channelId,
        CreateChannelInviteRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Gets all invites for a channel
    /// </summary>
    Task<IReadOnlyList<ChannelInviteDto>> GetInvitesAsync(
        Guid channelId,
        CancellationToken ct = default);

    /// <summary>
    ///     Deletes an invite from a channel
    /// </summary>
    Task DeleteInviteAsync(
        Guid channelId,
        string inviteCode,
        CancellationToken ct = default);

    /// <summary>
    ///     Joins a channel using an invite code
    /// </summary>
    Task<ChannelDto> JoinAsync(
        string inviteCode,
        CancellationToken ct = default);

    /// <summary>
    ///     Gets all categories in a server
    /// </summary>
    Task<IReadOnlyList<ChannelCategoryDto>> GetCategoriesAsync(
        Guid serverId,
        CancellationToken ct = default);

    /// <summary>
    ///     Creates a new category in a server
    /// </summary>
    Task<ChannelCategoryDto> CreateCategoryAsync(
        Guid serverId,
        CreateChannelCategoryRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Updates an existing category
    /// </summary>
    Task<ChannelCategoryDto> UpdateCategoryAsync(
        Guid categoryId,
        UpdateChannelCategoryRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Deletes a category
    /// </summary>
    Task DeleteCategoryAsync(
        Guid categoryId,
        CancellationToken ct = default);
}
