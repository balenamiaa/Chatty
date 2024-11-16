using Chatty.Backend.Services.Common;
using Chatty.Shared.Models.Channels;
using Chatty.Shared.Models.Common;

namespace Chatty.Backend.Services.Channels;

public interface IChannelService : IService
{
    Task<Result<ChannelDto>> CreateAsync(Guid serverId, CreateChannelRequest request, CancellationToken ct = default);
    Task<Result<ChannelDto>> UpdateAsync(Guid channelId, UpdateChannelRequest request, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid channelId, CancellationToken ct = default);
    Task<Result<ChannelDto>> GetAsync(Guid channelId, CancellationToken ct = default);
    Task<Result<IReadOnlyList<ChannelDto>>> GetForServerAsync(Guid serverId, CancellationToken ct = default);
    Task<Result<bool>> AddMemberAsync(Guid channelId, Guid userId, CancellationToken ct = default);
    Task<Result<bool>> RemoveMemberAsync(Guid channelId, Guid userId, CancellationToken ct = default);
    Task<Result<bool>> CanAccessAsync(Guid userId, Guid channelId, CancellationToken ct = default);
}
