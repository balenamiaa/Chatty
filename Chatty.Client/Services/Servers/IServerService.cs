using Chatty.Shared.Models.Servers;

namespace Chatty.Client.Services;

/// <summary>
///     Service for managing servers (communities)
/// </summary>
public interface IServerService
{
    /// <summary>
    ///     Gets a server by ID
    /// </summary>
    Task<ServerDto> GetAsync(Guid serverId, CancellationToken ct = default);

    /// <summary>
    ///     Gets all servers the current user is a member of
    /// </summary>
    Task<IReadOnlyList<ServerDto>> GetJoinedServersAsync(CancellationToken ct = default);

    /// <summary>
    ///     Creates a new server
    /// </summary>
    Task<ServerDto> CreateAsync(CreateServerRequest request, CancellationToken ct = default);

    /// <summary>
    ///     Updates an existing server
    /// </summary>
    Task<ServerDto> UpdateAsync(
        Guid serverId,
        UpdateServerRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Deletes a server
    /// </summary>
    Task DeleteAsync(Guid serverId, CancellationToken ct = default);

    /// <summary>
    ///     Gets the members of a server
    /// </summary>
    Task<IReadOnlyList<ServerMemberDto>> GetMembersAsync(
        Guid serverId,
        CancellationToken ct = default);

    /// <summary>
    ///     Adds a member to a server
    /// </summary>
    Task<ServerMemberDto> AddMemberAsync(
        Guid serverId,
        Guid userId,
        Guid? roleId = null,
        CancellationToken ct = default);

    /// <summary>
    ///     Updates a member in a server
    /// </summary>
    Task<ServerMemberDto> UpdateMemberAsync(
        Guid serverId,
        Guid userId,
        UpdateServerMemberRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Removes a member from a server
    /// </summary>
    Task RemoveMemberAsync(Guid serverId, Guid userId, CancellationToken ct = default);

    /// <summary>
    ///     Kicks a member from a server
    /// </summary>
    Task KickMemberAsync(Guid serverId, Guid userId, CancellationToken ct = default);

    /// <summary>
    ///     Gets the roles in a server
    /// </summary>
    Task<IReadOnlyList<ServerRoleDto>> GetRolesAsync(
        Guid serverId,
        CancellationToken ct = default);

    /// <summary>
    ///     Creates a new role in a server
    /// </summary>
    Task<ServerRoleDto> CreateRoleAsync(
        Guid serverId,
        CreateServerRoleRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Updates an existing role
    /// </summary>
    Task<ServerRoleDto> UpdateRoleAsync(
        Guid serverId,
        Guid roleId,
        UpdateServerRoleRequest request,
        CancellationToken ct = default);

    /// <summary>
    ///     Deletes a role
    /// </summary>
    Task DeleteRoleAsync(Guid serverId, Guid roleId, CancellationToken ct = default);

    // TODO: Add methods for:
    // - Server invites
    // - Server bans
    // - Server templates
    // - Server discovery
}
