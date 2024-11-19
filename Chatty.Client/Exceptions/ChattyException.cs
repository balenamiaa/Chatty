using System.Net;

namespace Chatty.Client.Exceptions;

/// <summary>
///     Base exception for Chatty client errors
/// </summary>
public class ChattyException : Exception
{
    public ChattyException(string message, string code, Exception? innerException = null)
        : base(message, innerException) =>
        Code = code;

    public ChattyException(
        string message,
        string code,
        HttpStatusCode statusCode,
        Exception? innerException = null)
        : base(message, innerException)
    {
        Code = code;
        StatusCode = statusCode;
    }

    /// <summary>
    ///     Error code for this exception
    /// </summary>
    public string Code { get; }

    /// <summary>
    ///     HTTP status code if this was caused by an API error
    /// </summary>
    public HttpStatusCode? StatusCode { get; }
}
