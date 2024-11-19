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

        #region Server Operations

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

        #endregion

        #region Role Management

        // Get server roles
        group.MapGet("/{serverId}/roles", async (
            Guid serverId,
            IServerService serverService,
            CancellationToken ct) =>
        {
            var result = await serverService.GetRolesAsync(serverId, ct);

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

        // Update role
        group.MapPut("/{serverId}/roles/{roleId}", async (
            Guid serverId,
            Guid roleId,
            [FromBody] UpdateServerRoleRequest request,
            IServerService serverService,
            CancellationToken ct) =>
        {
            var result = await serverService.UpdateRoleAsync(serverId, roleId, request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Delete role
        group.MapDelete("/{serverId}/roles/{roleId}", async (
            Guid serverId,
            Guid roleId,
            IServerService serverService,
            CancellationToken ct) =>
        {
            var result = await serverService.DeleteRoleAsync(serverId, roleId, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        #endregion

        #region Member Management

        // Get server members
        group.MapGet("/{serverId}/members", async (
            Guid serverId,
            IServerService serverService,
            CancellationToken ct) =>
        {
            var result = await serverService.GetMembersAsync(serverId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Add member
        group.MapPost("/{serverId}/members", async (
            Guid serverId,
            [FromBody] AddServerMemberRequest request,
            IServerService serverService,
            CancellationToken ct) =>
        {
            var result = await serverService.AddMemberAsync(serverId, request.UserId, request.RoleId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Update member
        group.MapPut("/{serverId}/members/{userId}", async (
            Guid serverId,
            Guid userId,
            [FromBody] UpdateServerMemberRequest request,
            IServerService serverService,
            CancellationToken ct) =>
        {
            var result = await serverService.UpdateMemberAsync(serverId, userId, request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
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

        // Kick member
        group.MapPost("/{serverId}/members/{userId}/kick", async (
            Guid serverId,
            Guid userId,
            IServerService serverService,
            CancellationToken ct) =>
        {
            var result = await serverService.KickMemberAsync(serverId, userId, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        #endregion
    }
}
