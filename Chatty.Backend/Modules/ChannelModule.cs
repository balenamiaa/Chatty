using System.Security.Claims;

using Carter;

using Chatty.Backend.Services.Channels;
using Chatty.Shared.Models.Channels;

using Microsoft.AspNetCore.Mvc;

namespace Chatty.Backend.Modules;

public sealed class ChannelModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/channels")
            .RequireAuthorization();

        // Create channel
        group.MapPost("/", async (
            [FromBody] CreateChannelRequest request,
            IChannelService channelService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await channelService.CreateAsync(userId, request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Get channel
        group.MapGet("/{channelId}", async (
            Guid channelId,
            IChannelService channelService,
            CancellationToken ct) =>
        {
            var result = await channelService.GetAsync(channelId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Update channel
        group.MapPut("/{channelId}", async (
            Guid channelId,
            [FromBody] UpdateChannelRequest request,
            IChannelService channelService,
            CancellationToken ct) =>
        {
            var result = await channelService.UpdateAsync(channelId, request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Delete channel
        group.MapDelete("/{channelId}", async (
            Guid channelId,
            IChannelService channelService,
            CancellationToken ct) =>
        {
            var result = await channelService.DeleteAsync(channelId, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Get server channels
        group.MapGet("/server/{serverId}", async (
            Guid serverId,
            IChannelService channelService,
            CancellationToken ct) =>
        {
            var result = await channelService.GetForServerAsync(serverId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Add member
        group.MapPost("/{channelId}/members/{userId}", async (
            Guid channelId,
            Guid userId,
            IChannelService channelService,
            CancellationToken ct) =>
        {
            var result = await channelService.AddMemberAsync(channelId, userId, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Remove member
        group.MapDelete("/{channelId}/members/{userId}", async (
            Guid channelId,
            Guid userId,
            IChannelService channelService,
            CancellationToken ct) =>
        {
            var result = await channelService.RemoveMemberAsync(channelId, userId, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });
    }
}
