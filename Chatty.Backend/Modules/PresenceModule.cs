using System.Security.Claims;

using Carter;

using Chatty.Backend.Services.Presence;
using Chatty.Shared.Models.Users;

using Microsoft.AspNetCore.Mvc;

namespace Chatty.Backend.Modules;

public sealed class PresenceModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/presence")
            .RequireAuthorization();

        // Update user status
        group.MapPost("/status", async (
            [FromBody] UpdateStatusRequest request,
            IPresenceService presenceService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await presenceService.UpdateStatusAsync(userId, request.Status, request.StatusMessage, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Get user status
        group.MapGet("/users/{userId}", async (
            Guid userId,
            IPresenceService presenceService,
            CancellationToken ct) =>
        {
            var result = await presenceService.GetUserStatusAsync(userId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Get multiple users' status
        group.MapPost("/users/batch", async (
            [FromBody] IEnumerable<Guid> userIds,
            IPresenceService presenceService,
            CancellationToken ct) =>
        {
            var result = await presenceService.GetUsersStatusAsync(userIds, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });
    }
}
