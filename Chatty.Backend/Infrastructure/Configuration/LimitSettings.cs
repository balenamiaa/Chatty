namespace Chatty.Backend.Infrastructure.Configuration;

public sealed class LimitSettings
{
    public int MaxMessageLength { get; init; } = 4096;
    public int MaxChannelsPerServer { get; init; } = 500;
    public int MaxMembersPerServer { get; init; } = 1000;
    public int MaxServersPerUser { get; init; } = 100;
    public required RateLimitSettings RateLimits { get; init; }
}

public sealed class RateLimitSettings
{
    public RateLimit Messages { get; init; } = new();
    public RateLimit Uploads { get; init; } = new();
}

public sealed class RateLimit
{
    public int Points { get; init; } = 10;
    public int DurationSeconds { get; init; } = 60;
}