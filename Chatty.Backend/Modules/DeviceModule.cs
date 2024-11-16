using System.Security.Claims;

using Carter;

using Chatty.Backend.Services.Notifications;
using Chatty.Shared.Models.Devices;

using Microsoft.AspNetCore.Mvc;

namespace Chatty.Backend.Modules;

public sealed class DeviceModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/devices")
            .RequireAuthorization();

        // Register device
        group.MapPost("/", async (
            [FromBody] RegisterDeviceRequest request,
            INotificationService notificationService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await notificationService.RegisterDeviceAsync(userId, request, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Unregister device
        group.MapDelete("/{deviceToken}", async (
            string deviceToken,
            INotificationService notificationService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await notificationService.UnregisterDeviceAsync(userId, deviceToken, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });
    }
}
