using System.Security.Claims;
using Carter;
using Chatty.Backend.Services.Users;
using Chatty.Shared.Models.Users;
using Microsoft.AspNetCore.Identity.Data;
using Microsoft.AspNetCore.Mvc;
using ResetPasswordRequest = Chatty.Shared.Models.Users.ResetPasswordRequest;

namespace Chatty.Backend.Modules;

public sealed class UserModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/users");

        // Create user (register)
        group.MapPost("/", async (
            [FromBody] CreateUserRequest request,
            IUserService userService,
            CancellationToken ct) =>
        {
            var result = await userService.CreateAsync(request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Get current user
        group.MapGet("/me", async (
            IUserService userService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await userService.GetByIdAsync(userId, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        })
        .RequireAuthorization();

        // Update user
        group.MapPut("/me", async (
            [FromBody] UpdateUserRequest request,
            IUserService userService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await userService.UpdateAsync(userId, request, ct);

            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        })
        .RequireAuthorization();

        // Change password
        group.MapPost("/me/change-password", async (
            [FromBody] ChangePasswordRequest request,
            IUserService userService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await userService.ChangePasswordAsync(userId, request.CurrentPassword, request.NewPassword, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        })
        .RequireAuthorization();

        // Request password reset
        group.MapPost("/reset-password/request", async (
            [FromBody] RequestPasswordResetRequest request,
            IUserService userService,
            CancellationToken ct) =>
        {
            var result = await userService.RequestPasswordResetAsync(request.Email, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });

        // Reset password
        group.MapPost("/reset-password", async (
            [FromBody] ResetPasswordRequest request,
            IUserService userService,
            CancellationToken ct) =>
        {
            var result = await userService.ResetPasswordAsync(request.Email, request.ResetToken, request.NewPassword, ct);

            return result.IsSuccess
                ? Results.NoContent()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });
    }
}