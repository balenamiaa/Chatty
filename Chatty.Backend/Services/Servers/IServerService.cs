using Chatty.Backend.Services.Common;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Servers;

namespace Chatty.Backend.Services.Servers;

public interface IServerService : IService
{
    // Server operations
    Task<Result<ServerDto>> CreateAsync(Guid userId, CreateServerRequest request, CancellationToken ct = default);
    Task<Result<ServerDto>> UpdateAsync(Guid serverId, UpdateServerRequest request, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid serverId, CancellationToken ct = default);
    Task<Result<ServerDto>> GetAsync(Guid serverId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ServerDto>>> GetUserServersAsync(Guid userId, CancellationToken ct = default);

    // Role management
    Task<Result<ServerRoleDto>> CreateRoleAsync(Guid serverId, CreateServerRoleRequest request,
        CancellationToken ct = default);

    Task<Result<ServerRoleDto>> UpdateRoleAsync(Guid serverId, Guid roleId, UpdateServerRoleRequest request,
        CancellationToken ct = default);

    Task<Result<bool>> DeleteRoleAsync(Guid serverId, Guid roleId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ServerRoleDto>>> GetRolesAsync(Guid serverId, CancellationToken ct = default);

    // Member management
    Task<Result<IReadOnlyList<ServerMemberDto>>> GetMembersAsync(Guid serverId, CancellationToken ct = default);

    Task<Result<ServerMemberDto>> AddMemberAsync(Guid serverId, Guid userId, Guid? roleId = null,
        CancellationToken ct = default);

    Task<Result<ServerMemberDto>> UpdateMemberAsync(Guid serverId, Guid userId, UpdateServerMemberRequest request,
        CancellationToken ct = default);

    Task<Result<bool>> RemoveMemberAsync(Guid serverId, Guid userId, CancellationToken ct = default);
    Task<Result<bool>> KickMemberAsync(Guid serverId, Guid userId, CancellationToken ct = default);

    Task<Result<bool>> UpdateMemberRoleAsync(
        Guid serverId,
        Guid userId,
        Guid roleId,
        CancellationToken ct = default);
}
