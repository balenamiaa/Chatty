using FluentValidation;

using Microsoft.AspNetCore.Diagnostics;

namespace Chatty.Backend.Infrastructure.Validation;

public sealed class ValidationExceptionHandler(ILogger<ValidationExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is not ValidationException validationException)
        {
            return false;
        }

        logger.LogWarning(
            "Validation error occurred: {Message}",
            validationException.Message);

        var errors = validationException.Errors
            .GroupBy(x => x.PropertyName)
            .ToDictionary(
                g => g.Key,
                g => g.Select(e => e.ErrorMessage).ToArray());

        httpContext.Response.StatusCode = StatusCodes.Status400BadRequest;
        await httpContext.Response.WriteAsJsonAsync(new
        {
            type = "ValidationFailure",
            title = "One or more validation errors occurred",
            errors
        }, cancellationToken);

        return true;
    }
}
