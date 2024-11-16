using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Realtime.Events;
using Chatty.Shared.Models.Calls;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Realtime.Events;
using Chatty.Backend.Data.Models.Extensions;
using Microsoft.EntityFrameworkCore;

namespace Chatty.Backend.Services.Voice;

public sealed class VoiceService : IVoiceService
{
    private readonly ILogger<VoiceService> _logger;
    private readonly IEventBus _eventBus;
    private readonly ChattyDbContext _context;

    public VoiceService(
        ILogger<VoiceService> logger,
        IEventBus eventBus,
        ChattyDbContext context)
    {
        _logger = logger;
        _eventBus = eventBus;
        _context = context;
    }

    public async Task<Result<bool>> EnableVideoAsync(
        Guid callId,
        Guid userId,
        bool enabled,
        CancellationToken ct = default)
    {
        try
        {
            var participant = await _context.CallParticipants
                .FirstOrDefaultAsync(p =>
                    p.CallId == callId &&
                    p.UserId == userId &&
                    p.LeftAt == null, ct);

            if (participant is null)
                return Result<bool>.Failure(Error.NotFound("Participant not found"));

            participant.VideoEnabled = enabled;
            await _context.SaveChangesAsync(ct);

            await _eventBus.PublishAsync(
                new ParticipantVideoEnabledEvent(callId, userId, enabled),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to update video state for user {UserId} in call {CallId}",
                userId, callId);
            return Result<bool>.Failure(Error.Internal("Failed to update video state"));
        }
    }

    public async Task<Result<IReadOnlyList<CallParticipantDto>>> GetParticipantsAsync(
        Guid callId,
        CancellationToken ct = default)
    {
        try
        {
            var participants = await _context.CallParticipants
                .Include(p => p.User)
                .Where(p => p.CallId == callId && p.LeftAt == null)
                .ToListAsync(ct);

            return Result<IReadOnlyList<CallParticipantDto>>.Success(
                participants.Select(p => p.ToDto()).ToList());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get participants for call {CallId}", callId);
            return Result<IReadOnlyList<CallParticipantDto>>.Failure(
                Error.Internal("Failed to get participants"));
        }
    }

    public async Task<Result<CallDto>> InitiateCallAsync(
      Guid initiatorId,
      CreateCallRequest request,
      CancellationToken ct = default)
    {
        try
        {
            var call = new Call
            {
                InitiatorId = initiatorId,
                ChannelId = request.ChannelId,
                CallType = request.ChannelId.HasValue ? CallType.Voice : CallType.Video,
                Status = CallStatus.Initiated
            };

            _context.Calls.Add(call);
            await _context.SaveChangesAsync(ct);

            // Add initiator as first participant
            var participant = new CallParticipant
            {
                CallId = call.Id,
                UserId = initiatorId
            };

            _context.CallParticipants.Add(participant);
            await _context.SaveChangesAsync(ct);

            await _context.Entry(call)
                .Reference(c => c.Initiator)
                .LoadAsync(ct);

            var callDto = call.ToDto();

            // Publish call started event
            await _eventBus.PublishAsync(new CallStartedEvent(callDto), ct);

            return Result<CallDto>.Success(callDto);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to initiate call");
            return Result<CallDto>.Failure(Error.Internal("Failed to initiate call"));
        }
    }

    public Task<Result<bool>> JoinCallAsync(Guid callId, Guid userId, bool withVideo = false, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool>> LeaveCallAsync(Guid callId, Guid userId, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool>> MuteParticipantAsync(Guid callId, Guid userId, bool muted, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public Task<Result<bool>> UpdateCallStatusAsync(Guid callId, UpdateCallRequest request, CancellationToken ct = default)
    {
        throw new NotImplementedException();
    }

    public async Task<Result<bool>> SendSignalingMessageAsync(
        Guid callId,
        Guid userId,
        SignalingMessage message,
        CancellationToken ct = default)
    {
        try
        {
            // Verify call exists and user is a participant
            var call = await _context.Calls
                .Include(c => c.Participants)
                .FirstOrDefaultAsync(c => c.Id == callId, ct);

            if (call is null)
                return Result<bool>.Failure(Error.NotFound("Call not found"));

            var isParticipant = call.Participants.Any(p => p.UserId == userId && p.LeftAt == null);
            if (!isParticipant)
                return Result<bool>.Failure(Error.Forbidden("User is not a participant in this call"));

            // The actual signaling message is handled by the hub, so we just validate here
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send signaling message in call {CallId} from user {UserId}", callId, userId);
            return Result<bool>.Failure(Error.Internal("Failed to send signaling message"));
        }
    }

}