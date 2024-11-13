using System.Security.Claims;
using Carter;
using Chatty.Backend.Services.Stickers;
using Chatty.Shared.Models.Stickers;
using Microsoft.AspNetCore.Mvc;

namespace Chatty.Backend.Modules;

public sealed class StickerModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/stickers")
            .RequireAuthorization();

        group.MapPost("/packs", async (
            [FromBody] CreateStickerPackRequest request,
            IStickerService stickerService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await stickerService.CreatePackAsync(userId, request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        group.MapPost("/packs/{packId}/stickers", async (
            string packId,
            [FromForm] string name,
            [FromForm] string? description,
            [FromForm] string[]? tags,
            IFormFile file,
            IStickerService stickerService,
            CancellationToken ct) =>
        {
            await using var stream = file.OpenReadStream();
            var result = await stickerService.AddStickerAsync(packId, stream, name, description, tags, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        group.MapDelete("/stickers/{stickerId}", async (
            Guid stickerId,
            IStickerService stickerService,
            CancellationToken ct) =>
        {
            var result = await stickerService.DeleteStickerAsync(stickerId, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        group.MapDelete("/packs/{packId}", async (
            string packId,
            IStickerService stickerService,
            CancellationToken ct) =>
        {
            var result = await stickerService.DeletePackAsync(packId, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        group.MapPost("/packs/{packId}/publish", async (
            string packId,
            IStickerService stickerService,
            CancellationToken ct) =>
        {
            var result = await stickerService.PublishPackAsync(packId, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        group.MapPost("/packs/{packId}/servers/{serverId}/enable", async (
            string packId,
            Guid serverId,
            IStickerService stickerService,
            CancellationToken ct) =>
        {
            var result = await stickerService.EnablePackForServerAsync(packId, serverId, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        group.MapPost("/packs/{packId}/servers/{serverId}/disable", async (
            string packId,
            Guid serverId,
            IStickerService stickerService,
            CancellationToken ct) =>
        {
            var result = await stickerService.DisablePackForServerAsync(packId, serverId, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        group.MapGet("/packs", async (
            [FromQuery] Guid? serverId,
            IStickerService stickerService,
            CancellationToken ct) =>
        {
            var result = await stickerService.GetAvailablePacksAsync(serverId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        group.MapGet("/packs/me", async (
            IStickerService stickerService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await stickerService.GetUserPacksAsync(userId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });
    }
}