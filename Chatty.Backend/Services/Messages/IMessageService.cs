using Chatty.Backend.Services.Common;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Messages;

namespace Chatty.Backend.Services.Messages;

public interface IMessageService : IService
{
    Task<Result<MessageDto>> CreateAsync(Guid userId, CreateMessageRequest request, CancellationToken ct = default);

    Task<Result<DirectMessageDto>> CreateDirectAsync(Guid userId, CreateDirectMessageRequest request,
        CancellationToken ct = default);

    Task<Result<bool>> DeleteAsync(Guid messageId, Guid userId, CancellationToken ct = default);
    Task<Result<bool>> DeleteDirectAsync(Guid messageId, Guid userId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<MessageDto>>> GetChannelMessagesAsync(Guid channelId, int limit, DateTime? before = null,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<DirectMessageDto>>> GetDirectMessagesAsync(Guid userId, Guid otherUserId, int limit,
        DateTime? before = null, CancellationToken ct = default);

    Task<Result<MessageDto>> UpdateAsync(
        Guid messageId,
        Guid userId,
        UpdateMessageRequest request,
        CancellationToken ct = default);

    Task<Result<DirectMessageDto>> UpdateDirectAsync(
        Guid messageId,
        Guid userId,
        UpdateDirectMessageRequest request,
        CancellationToken ct = default);
}