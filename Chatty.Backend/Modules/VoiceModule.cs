using System.Security.Claims;
using Carter;
using Chatty.Backend.Services.Voice;
using Chatty.Shared.Models.Calls;
using Microsoft.AspNetCore.Mvc;

namespace Chatty.Backend.Modules;

public sealed class VoiceModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/voice")
            .RequireAuthorization();

        // Initiate call
        group.MapPost("/calls", async (
            [FromBody] CreateCallRequest request,
            IVoiceService voiceService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await voiceService.InitiateCallAsync(userId, request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Join call
        group.MapPost("/calls/{callId}/join", async (
            Guid callId,
            [FromQuery] bool withVideo,
            IVoiceService voiceService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await voiceService.JoinCallAsync(callId, userId, withVideo, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Leave call
        group.MapPost("/calls/{callId}/leave", async (
            Guid callId,
            IVoiceService voiceService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await voiceService.LeaveCallAsync(callId, userId, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Update call status
        group.MapPut("/calls/{callId}", async (
            Guid callId,
            [FromBody] UpdateCallRequest request,
            IVoiceService voiceService,
            CancellationToken ct) =>
        {
            var result = await voiceService.UpdateCallStatusAsync(callId, request, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Mute participant
        group.MapPost("/calls/{callId}/participants/{userId}/mute", async (
            Guid callId,
            Guid userId,
            [FromQuery] bool muted,
            IVoiceService voiceService,
            CancellationToken ct) =>
        {
            var result = await voiceService.MuteParticipantAsync(callId, userId, muted, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Enable/disable video
        group.MapPost("/calls/{callId}/participants/{userId}/video", async (
            Guid callId,
            Guid userId,
            [FromQuery] bool enabled,
            IVoiceService voiceService,
            CancellationToken ct) =>
        {
            var result = await voiceService.EnableVideoAsync(callId, userId, enabled, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Get call participants
        group.MapGet("/calls/{callId}/participants", async (
            Guid callId,
            IVoiceService voiceService,
            CancellationToken ct) =>
        {
            var result = await voiceService.GetParticipantsAsync(callId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });
    }
}