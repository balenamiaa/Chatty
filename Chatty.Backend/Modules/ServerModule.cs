using System.Security.Claims;
using Carter;
using Chatty.Backend.Services.Servers;
using Chatty.Shared.Models.Servers;
using Microsoft.AspNetCore.Mvc;

namespace Chatty.Backend.Modules;

public sealed class ServerModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/servers")
            .RequireAuthorization();

        // Create server
        group.MapPost("/", async (
            [FromBody] CreateServerRequest request,
            IServerService serverService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await serverService.CreateAsync(userId, request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Get server
        group.MapGet("/{serverId}", async (
            Guid serverId,
            IServerService serverService,
            CancellationToken ct) =>
        {
            var result = await serverService.GetAsync(serverId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Update server
        group.MapPut("/{serverId}", async (
            Guid serverId,
            [FromBody] UpdateServerRequest request,
            IServerService serverService,
            CancellationToken ct) =>
        {
            var result = await serverService.UpdateAsync(serverId, request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Delete server
        group.MapDelete("/{serverId}", async (
            Guid serverId,
            IServerService serverService,
            CancellationToken ct) =>
        {
            var result = await serverService.DeleteAsync(serverId, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Get user's servers
        group.MapGet("/me", async (
            IServerService serverService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await serverService.GetUserServersAsync(userId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Create role
        group.MapPost("/{serverId}/roles", async (
            Guid serverId,
            [FromBody] CreateServerRoleRequest request,
            IServerService serverService,
            CancellationToken ct) =>
        {
            var result = await serverService.CreateRoleAsync(serverId, request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Add member
        group.MapPost("/{serverId}/members/{userId}", async (
            Guid serverId,
            Guid userId,
            [FromQuery] Guid? roleId,
            IServerService serverService,
            CancellationToken ct) =>
        {
            var result = await serverService.AddMemberAsync(serverId, userId, roleId, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Remove member
        group.MapDelete("/{serverId}/members/{userId}", async (
            Guid serverId,
            Guid userId,
            IServerService serverService,
            CancellationToken ct) =>
        {
            var result = await serverService.RemoveMemberAsync(serverId, userId, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Update member role
        group.MapPut("/{serverId}/members/{userId}/role", async (
            Guid serverId,
            Guid userId,
            [FromBody] Guid roleId,
            IServerService serverService,
            CancellationToken ct) =>
        {
            var result = await serverService.UpdateMemberRoleAsync(serverId, userId, roleId, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });
    }
}