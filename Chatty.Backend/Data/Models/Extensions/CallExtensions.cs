using Chatty.Shared.Models.Calls;

namespace Chatty.Backend.Data.Models.Extensions;

public static class CallExtensions
{
    public static CallDto ToDto(this Call call) => new(
        call.Id,
        call.ChannelId,
        call.Channel?.ToDto(),
        call.Initiator.ToDto(),
        call.CallType,
        call.StartedAt,
        call.EndedAt,
        call.Status,
        call.Participants.Select(p => p.ToDto()).ToList());

    public static CallParticipantDto ToDto(this CallParticipant participant) => new(
        participant.Id,
        participant.CallId,
        participant.User.ToDto(),
        participant.JoinedAt,
        participant.LeftAt,
        participant.Muted,
        participant.VideoEnabled);
}
