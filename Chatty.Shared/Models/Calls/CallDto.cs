using Chatty.Shared.Models.Channels;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Users;

namespace Chatty.Shared.Models.Calls;

public sealed record CallDto(
    Guid Id,
    Guid? ChannelId,
    ChannelDto? Channel,
    UserDto Initiator,
    CallType CallType,
    DateTime StartedAt,
    DateTime? EndedAt,
    CallStatus Status,
    IReadOnlyList<CallParticipantDto> Participants);
