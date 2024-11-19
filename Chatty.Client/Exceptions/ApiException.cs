using System.Net;

namespace Chatty.Client.Exceptions;

/// <summary>
///     Exception thrown when an API request fails
/// </summary>
public class ApiException(
    string message,
    HttpStatusCode statusCode,
    string code = "API_ERROR",
    Exception? innerException = null)
    : ChattyException(message, code, statusCode, innerException)
{
    public static ApiException FromStatusCode(HttpStatusCode statusCode, string? details = null)
    {
        var message = statusCode switch
        {
            HttpStatusCode.BadRequest => "The request was invalid",
            HttpStatusCode.Unauthorized => "Authentication is required",
            HttpStatusCode.Forbidden => "You don't have permission to perform this action",
            HttpStatusCode.NotFound => "The requested resource was not found",
            HttpStatusCode.Conflict => "The request conflicts with the current state",
            HttpStatusCode.TooManyRequests => "Too many requests, please try again later",
            HttpStatusCode.InternalServerError => "An internal server error occurred",
            _ => "An unexpected error occurred"
        };

        if (details is not null)
        {
            message += $": {details}";
        }

        var code = statusCode switch
        {
            HttpStatusCode.BadRequest => "BAD_REQUEST",
            HttpStatusCode.Unauthorized => "UNAUTHORIZED",
            HttpStatusCode.Forbidden => "FORBIDDEN",
            HttpStatusCode.NotFound => "NOT_FOUND",
            HttpStatusCode.Conflict => "CONFLICT",
            HttpStatusCode.TooManyRequests => "RATE_LIMITED",
            HttpStatusCode.InternalServerError => "SERVER_ERROR",
            _ => "API_ERROR"
        };

        return new ApiException(message, statusCode, code);
    }
}
