namespace Chatty.Client.Http;

/// <summary>
///     Error response from the API
/// </summary>
internal record ApiErrorResponse(
    string Message,
    string Code,
    string? Details = null,
    Dictionary<string, string[]>? ValidationErrors = null);
