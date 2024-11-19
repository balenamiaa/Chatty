using Chatty.Shared.Models.Calls;

namespace Chatty.Shared.Realtime.Events;

public sealed record CallStartedEvent(CallDto Call);

public sealed record CallEndedEvent(Guid CallId);

public sealed record ParticipantJoinedEvent(Guid CallId, CallParticipantDto Participant);

public sealed record ParticipantLeftEvent(Guid CallId, Guid UserId);

public sealed record ParticipantMutedEvent(Guid CallId, Guid UserId, bool IsMuted);

public sealed record ParticipantVideoEnabledEvent(Guid CallId, Guid UserId, bool IsEnabled);
