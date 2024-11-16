using Chatty.Shared.Models.Users;

namespace Chatty.Shared.Models.Calls;

public sealed record CallParticipantDto(
    Guid Id,
    Guid CallId,
    UserDto User,
    DateTime JoinedAt,
    DateTime? LeftAt,
    bool Muted,
    bool VideoEnabled);
