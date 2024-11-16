namespace Chatty.Shared.Models.Common;

public sealed record HubError(string Code, string Message)
{
    public static HubError Unauthorized(string message) => new(nameof(Unauthorized), message);
    public static HubError ValidationFailed(string message) => new(nameof(ValidationFailed), message);
    public static HubError NotFound(string message) => new(nameof(NotFound), message);
    public static HubError RateLimitExceeded(string message) => new(nameof(RateLimitExceeded), message);
    public static HubError ConnectionError(string message) => new(nameof(ConnectionError), message);
    public static HubError Internal(string message) => new(nameof(Internal), message);
}
