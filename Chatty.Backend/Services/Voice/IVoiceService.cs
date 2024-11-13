using Chatty.Backend.Services.Common;
using Chatty.Shared.Models.Calls;
using Chatty.Shared.Models.Common;

namespace Chatty.Backend.Services.Voice;

public interface IVoiceService : IService
{
    Task<Result<CallDto>> InitiateCallAsync(Guid initiatorId, CreateCallRequest request,
        CancellationToken ct = default);

    Task<Result<bool>> JoinCallAsync(Guid callId, Guid userId, bool withVideo = false, CancellationToken ct = default);
    Task<Result<bool>> LeaveCallAsync(Guid callId, Guid userId, CancellationToken ct = default);
    Task<Result<bool>> UpdateCallStatusAsync(Guid callId, UpdateCallRequest request, CancellationToken ct = default);
    Task<Result<bool>> MuteParticipantAsync(Guid callId, Guid userId, bool muted, CancellationToken ct = default);
    Task<Result<bool>> EnableVideoAsync(Guid callId, Guid userId, bool enabled, CancellationToken ct = default);
    Task<Result<IReadOnlyList<CallParticipantDto>>> GetParticipantsAsync(Guid callId, CancellationToken ct = default);
}