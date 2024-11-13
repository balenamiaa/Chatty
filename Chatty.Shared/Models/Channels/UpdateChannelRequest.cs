namespace Chatty.Shared.Models.Channels;

public sealed record UpdateChannelRequest(
    string? Name,
    string? Topic,
    int? Position,
    int? RateLimitPerUser);