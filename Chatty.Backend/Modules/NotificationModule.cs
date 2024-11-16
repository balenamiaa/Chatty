using Carter;

using Chatty.Backend.Services.Notifications;
using Chatty.Shared.Models.Notifications;

using Microsoft.AspNetCore.Mvc;

namespace Chatty.Backend.Modules;

public sealed class NotificationModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/notifications")
            .RequireAuthorization();

        // Send notification to user
        group.MapPost("/users/{userId}", async (
            Guid userId,
            [FromBody] SendNotificationRequest request,
            INotificationService notificationService,
            CancellationToken ct) =>
        {
            var result = await notificationService.SendToUserAsync(
                userId,
                request.Title,
                request.Body,
                request.Data,
                ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Send notification to device
        group.MapPost("/devices/{deviceToken}", async (
            string deviceToken,
            [FromBody] SendNotificationRequest request,
            INotificationService notificationService,
            CancellationToken ct) =>
        {
            var result = await notificationService.SendToDeviceAsync(
                deviceToken,
                request.Title,
                request.Body,
                request.Data,
                ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Send notification to multiple devices
        group.MapPost("/devices", async (
            [FromBody] SendMultiNotificationRequest request,
            INotificationService notificationService,
            CancellationToken ct) =>
        {
            var result = await notificationService.SendToDevicesAsync(
                request.DeviceTokens,
                request.Title,
                request.Body,
                request.Data,
                ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });
    }
}
