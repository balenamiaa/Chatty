namespace Chatty.Shared.Models.Common;

public readonly record struct Error(string Code, string Message)
{
    public static Error NotFound(string message) => new(nameof(NotFound), message);
    public static Error Validation(string message) => new(nameof(Validation), message);
    public static Error Unauthorized(string message) => new(nameof(Unauthorized), message);
    public static Error Forbidden(string message) => new(nameof(Forbidden), message);
    public static Error Conflict(string message) => new(nameof(Conflict), message);
    public static Error Internal(string message) => new(nameof(Internal), message);
    public static Error InvalidInput(string message) => new(nameof(InvalidInput), message);
    public static Error TooManyRequests(string message) => new(nameof(TooManyRequests), message);
}