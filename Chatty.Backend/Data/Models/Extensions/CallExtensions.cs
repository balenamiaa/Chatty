using Chatty.Shared.Models.Calls;

namespace Chatty.Backend.Data.Models.Extensions;

public static class CallExtensions
{
    public static CallDto ToDto(this Call call) => new(
        Id: call.Id,
        ChannelId: call.ChannelId,
        Channel: call.Channel?.ToDto(),
        Initiator: call.Initiator.ToDto(),
        CallType: call.CallType,
        StartedAt: call.StartedAt,
        EndedAt: call.EndedAt,
        Status: call.Status,
        Participants: call.Participants.Select(p => p.ToDto()).ToList());

    public static CallParticipantDto ToDto(this CallParticipant participant) => new(
        Id: participant.Id,
        CallId: participant.CallId,
        User: participant.User.ToDto(),
        JoinedAt: participant.JoinedAt,
        LeftAt: participant.LeftAt,
        Muted: participant.Muted,
        VideoEnabled: participant.VideoEnabled);
}
