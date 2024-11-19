using Chatty.Shared.Models.Channels;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Users;

using MessagePack;

namespace Chatty.Shared.Models.Calls;

[MessagePackObject]
public sealed record CallDto
{
    public CallDto(
        Guid Id,
        Guid? ChannelId,
        ChannelDto? Channel,
        UserDto? Initiator,
        CallType CallType,
        DateTime StartedAt,
        DateTime? EndedAt,
        CallStatus Status,
        IReadOnlyList<CallParticipantDto> Participants)
    {
        this.Id = Id;
        this.ChannelId = ChannelId;
        this.Channel = Channel;
        this.Initiator = Initiator;
        this.CallType = CallType;
        this.StartedAt = StartedAt;
        this.EndedAt = EndedAt;
        this.Status = Status;
        this.Participants = Participants;
    }

    [Key(0)] public Guid Id { get; init; }

    [Key(1)] public Guid? ChannelId { get; init; }

    [Key(2)] public ChannelDto? Channel { get; init; }

    [Key(3)] public UserDto? Initiator { get; init; }

    [Key(4)] public CallType CallType { get; init; }

    [Key(5)] public DateTime StartedAt { get; init; }

    [Key(6)] public DateTime? EndedAt { get; init; }

    [Key(7)] public CallStatus Status { get; init; }

    [Key(8)] public IReadOnlyList<CallParticipantDto> Participants { get; init; } = [];
}
