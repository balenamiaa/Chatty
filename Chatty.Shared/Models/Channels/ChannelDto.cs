using Chatty.Shared.Models.Enums;

namespace Chatty.Shared.Models.Channels;

public sealed record ChannelDto(
    Guid Id,
    Guid? ServerId,
    string Name,
    string? Topic,
    bool IsPrivate,
    ChannelType ChannelType,
    int Position,
    int RateLimitPerUser,
    DateTime CreatedAt,
    DateTime UpdatedAt);
