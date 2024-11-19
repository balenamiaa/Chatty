using System.Security.Claims;

using Carter;

using Chatty.Backend.Services.Messages;
using Chatty.Shared.Models.Enums;

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

        // Get message by ID
        group.MapGet("/{messageId}", async (
            Guid messageId,
            IMessageService messageService,
            CancellationToken ct) =>
        {
            var result = await messageService.GetChannelMessageAsync(messageId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Get message reactions
        group.MapGet("/{messageId}/reactions", async (
            Guid messageId,
            IMessageService messageService,
            CancellationToken ct) =>
        {
            var result = await messageService.GetMessageReactionsAsync(messageId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Get message reactions by type
        group.MapGet("/{messageId}/reactions/{type}", async (
            Guid messageId,
            ReactionType type,
            IMessageService messageService,
            CancellationToken ct) =>
        {
            var result = await messageService.GetMessageReactionsByTypeAsync(messageId, type, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Get message replies
        group.MapGet("/{messageId}/replies", async (
            Guid messageId,
            IMessageService messageService,
            CancellationToken ct,
            [FromQuery] int limit = 50,
            [FromQuery] DateTime? before = null) =>
        {
            var result = await messageService.GetMessageRepliesAsync(messageId, limit, before, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Get reply count
        group.MapGet("/{messageId}/replies/count", async (
            Guid messageId,
            IMessageService messageService,
            CancellationToken ct) =>
        {
            var result = await messageService.GetMessageReplyCountAsync(messageId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Get user's reaction to a message
        group.MapGet("/{messageId}/reactions/users/{userId}", async (
            Guid messageId,
            Guid userId,
            IMessageService messageService,
            CancellationToken ct) =>
        {
            var result = await messageService.GetUserReactionAsync(messageId, userId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Get parent message
        group.MapGet("/{messageId}/parent", async (
            Guid messageId,
            IMessageService messageService,
            CancellationToken ct) =>
        {
            var result = await messageService.GetParentMessageAsync(messageId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Get user mentions
        group.MapGet("/mentions/{userId}", async (
            Guid userId,
            IMessageService messageService,
            CancellationToken ct,
            [FromQuery] int limit = 50,
            [FromQuery] DateTime? before = null,
            [FromQuery] DateTime? after = null) =>
        {
            var result = await messageService.GetUserMentionsAsync(userId, limit, before, after, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });
    }
}
