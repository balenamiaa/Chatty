using System.Security.Claims;
using Carter;
using Chatty.Backend.Services.Messages;
using Microsoft.AspNetCore.Mvc;

namespace Chatty.Backend.Modules;

public sealed class MessageModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/messages")
            .RequireAuthorization();

        // Get channel messages (history)
        group.MapGet("/channel/{channelId}", async (
            Guid channelId,
            IMessageService messageService,
            CancellationToken ct,
            [FromQuery] int limit = 50,
            [FromQuery] DateTime? before = null) =>
        {
            var result = await messageService.GetChannelMessagesAsync(channelId, limit, before, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Get direct messages (history)
        group.MapGet("/direct/{otherUserId}", async (
            Guid otherUserId,
            IMessageService messageService,
            ClaimsPrincipal user,
            CancellationToken ct,
            [FromQuery] int limit = 50,
            [FromQuery] DateTime? before = null) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await messageService.GetDirectMessagesAsync(userId, otherUserId, limit, before, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });
    }
}