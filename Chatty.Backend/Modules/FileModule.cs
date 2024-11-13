using System.Security.Claims;
using Carter;
using Chatty.Backend.Services.Files;
using Chatty.Shared.Models.Attachments;
using Microsoft.AspNetCore.Mvc;

namespace Chatty.Backend.Modules;

public sealed class FileModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/files")
            .RequireAuthorization();

        // Upload file
        group.MapPost("/", async (
            IFormFile file,
            [FromForm] CreateAttachmentRequest request,
            IFileService fileService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);

            await using var stream = file.OpenReadStream();
            var result = await fileService.UploadAsync(userId, request, stream, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Download file
        group.MapGet("/{attachmentId}/download", async (
            Guid attachmentId,
            IFileService fileService,
            CancellationToken ct) =>
        {
            var result = await fileService.DownloadAsync(attachmentId, ct);

            if (!result.IsSuccess)
                return Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);

            return Results.File(
                result.Value,
                "application/octet-stream",
                $"attachment_{attachmentId}");
        });

        // Get thumbnail
        group.MapGet("/{attachmentId}/thumbnail", async (
            Guid attachmentId,
            IFileService fileService,
            CancellationToken ct) =>
        {
            var result = await fileService.GetThumbnailUrlAsync(attachmentId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Delete file
        group.MapDelete("/{attachmentId}", async (
            Guid attachmentId,
            IFileService fileService,
            CancellationToken ct) =>
        {
            var result = await fileService.DeleteAsync(attachmentId, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });
    }
}