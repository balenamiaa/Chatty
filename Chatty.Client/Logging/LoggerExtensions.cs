using System.Runtime.CompilerServices;

using Microsoft.Extensions.Logging;

namespace Chatty.Client.Logging;

/// <summary>
///     Extension methods for ILogger to provide structured logging capabilities
/// </summary>
public static class LoggerExtensions
{
    private static string GetCallerInfo(string? memberName = null, string? sourceFilePath = null,
        int? sourceLineNumber = null)
    {
        var fileName = sourceFilePath != null ? Path.GetFileName(sourceFilePath) : "";
        return $"{fileName}:{sourceLineNumber} {memberName}";
    }

    public static void LogMethodEntry(
        this ILogger logger,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int? sourceLineNumber = null) =>
        logger.LogDebug(
            "Entering method {CallerInfo}",
            GetCallerInfo(memberName, sourceFilePath, sourceLineNumber));

    public static void LogMethodExit(
        this ILogger logger,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int? sourceLineNumber = null) =>
        logger.LogDebug(
            "Exiting method {CallerInfo}",
            GetCallerInfo(memberName, sourceFilePath, sourceLineNumber));

    public static void LogHttpRequest(
        this ILogger logger,
        string method,
        string url,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int? sourceLineNumber = null) =>
        logger.LogInformation(
            "[{CallerInfo}] HTTP {Method} {Url}",
            GetCallerInfo(memberName, sourceFilePath, sourceLineNumber),
            method,
            url);

    public static void LogHttpResponse(
        this ILogger logger,
        string method,
        string url,
        int statusCode,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int? sourceLineNumber = null) =>
        logger.LogInformation(
            "[{CallerInfo}] HTTP {Method} {Url} returned {StatusCode}",
            GetCallerInfo(memberName, sourceFilePath, sourceLineNumber),
            method,
            url,
            statusCode);

    public static void LogCacheHit(
        this ILogger logger,
        string key,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int? sourceLineNumber = null) =>
        logger.LogDebug(
            "[{CallerInfo}] Cache hit for key {Key}",
            GetCallerInfo(memberName, sourceFilePath, sourceLineNumber),
            key);

    public static void LogCacheMiss(
        this ILogger logger,
        string key,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int? sourceLineNumber = null) =>
        logger.LogDebug(
            "[{CallerInfo}] Cache miss for key {Key}",
            GetCallerInfo(memberName, sourceFilePath, sourceLineNumber),
            key);

    public static void LogStateChange(
        this ILogger logger,
        string key,
        string? value,
        [CallerMemberName] string? memberName = null,
        [CallerFilePath] string? sourceFilePath = null,
        [CallerLineNumber] int? sourceLineNumber = null) =>
        logger.LogDebug(
            "[{CallerInfo}] State change for key {Key}: {Value}",
            GetCallerInfo(memberName, sourceFilePath, sourceLineNumber),
            key,
            value ?? "null");

    public static void Debug(
        this ILogger logger,
        string message,
        params (string Key, object? Value)[] properties) =>
        LogWithProperties(logger, LogLevel.Debug, message, properties);

    public static void Info(
        this ILogger logger,
        string message,
        params (string Key, object? Value)[] properties) =>
        LogWithProperties(logger, LogLevel.Information, message, properties);

    public static void Warning(
        this ILogger logger,
        string message,
        Exception? exception = null,
        params (string Key, object? Value)[] properties) =>
        LogWithProperties(logger, LogLevel.Warning, message, properties, exception);

    public static void Error(
        this ILogger logger,
        string message,
        Exception? exception = null,
        params (string Key, object? Value)[] properties) =>
        LogWithProperties(logger, LogLevel.Error, message, properties, exception);

    public static void Critical(
        this ILogger logger,
        string message,
        Exception? exception = null,
        params (string Key, object? Value)[] properties) =>
        LogWithProperties(logger, LogLevel.Critical, message, properties, exception);

    private static void LogWithProperties(
        ILogger logger,
        LogLevel logLevel,
        string message,
        (string Key, object? Value)[] properties,
        Exception? exception = null,
        params object[] args)
    {
        if (!logger.IsEnabled(logLevel))
        {
            return;
        }

        var state = new Dictionary<string, object?>();
        foreach (var (key, value) in properties)
        {
            state[key] = value;
        }

        using (logger.BeginScope(state))
        {
            logger.Log(logLevel, exception, message, args);
        }
    }
}
