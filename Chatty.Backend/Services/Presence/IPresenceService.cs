using Chatty.Backend.Services.Common;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Enums;

namespace Chatty.Backend.Services.Presence;

public interface IPresenceService : IService
{
    Task<Result<bool>> UpdateStatusAsync(Guid userId, UserStatus status, string? statusMessage = null,
        CancellationToken ct = default);

    Task<Result<bool>> UpdateLastSeenAsync(Guid userId, CancellationToken ct = default);
    Task<Result<UserStatus>> GetUserStatusAsync(Guid userId, CancellationToken ct = default);

    Task<Result<IReadOnlyDictionary<Guid, UserStatus>>> GetUsersStatusAsync(IEnumerable<Guid> userIds,
        CancellationToken ct = default);
}
