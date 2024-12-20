using Carter;

using Chatty.Backend.Services.Auth;
using Chatty.Shared.Models.Auth;

using Microsoft.AspNetCore.Mvc;

namespace Chatty.Backend.Modules;

public sealed class AuthModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/auth");

        group.MapPost("/login", async (
            AuthRequest request,
            IAuthService authService,
            CancellationToken ct) =>
        {
            var result = await authService.AuthenticateAsync(request, ct);
            return result.IsSuccess
                ? Results.Ok(result.Value)
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });


        group.MapPost("/logout", async (
            IAuthService authService,
            [FromQuery] string token,
            CancellationToken ct) =>
        {
            var result = await authService.LogoutAsync(token, ct);
            return result.IsSuccess
                ? Results.Ok()
                : Results.Problem(
                    title: result.Error.Code,
                    detail: result.Error.Message,
                    statusCode: StatusCodes.Status400BadRequest);
        });
    }
}
