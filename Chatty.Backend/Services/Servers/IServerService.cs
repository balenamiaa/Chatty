using Chatty.Backend.Services.Common;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Servers;

namespace Chatty.Backend.Services.Servers;

public interface IServerService : IService
{
    Task<Result<ServerDto>> CreateAsync(Guid userId, CreateServerRequest request, CancellationToken ct = default);
    Task<Result<ServerDto>> UpdateAsync(Guid serverId, UpdateServerRequest request, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid serverId, CancellationToken ct = default);
    Task<Result<ServerDto>> GetAsync(Guid serverId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ServerDto>>> GetUserServersAsync(Guid userId, CancellationToken ct = default);
    Task<Result<ServerRoleDto>> CreateRoleAsync(Guid serverId, CreateServerRoleRequest request, CancellationToken ct = default);
    Task<Result<bool>> AddMemberAsync(Guid serverId, Guid userId, Guid? roleId = null, CancellationToken ct = default);
    Task<Result<bool>> RemoveMemberAsync(Guid serverId, Guid userId, CancellationToken ct = default);
    Task<Result<bool>> UpdateMemberRoleAsync(Guid serverId, Guid userId, Guid roleId, CancellationToken ct = default);
}
