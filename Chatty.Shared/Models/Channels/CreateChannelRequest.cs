using Chatty.Shared.Models.Enums;

namespace Chatty.Shared.Models.Channels;

public sealed record CreateChannelRequest(
    string Name,
    string? Topic,
    bool IsPrivate,
    ChannelType ChannelType,
    int Position,
    int RateLimitPerUser);
