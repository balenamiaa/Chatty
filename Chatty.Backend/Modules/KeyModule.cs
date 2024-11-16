using System.Security.Claims;

using Carter;

using Chatty.Backend.Security.KeyBackup;
using Chatty.Backend.Security.KeyRotation;

using Microsoft.AspNetCore.Mvc;

namespace Chatty.Backend.Modules;

public sealed class KeyModule : ICarterModule
{
    public void AddRoutes(IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/keys")
            .RequireAuthorization();

        // Get current key
        group.MapGet("/current", async (
            IKeyRotationService keyRotationService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var key = await keyRotationService.GetCurrentKeyAsync(userId, ct);
            return Results.Ok(Convert.ToBase64String(key));
        });

        // Rotate key
        group.MapPost("/rotate", async (
            IKeyRotationService keyRotationService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var (key, version) = await keyRotationService.RotateKeyAsync(userId, ct);
            return Results.Ok(new { Key = Convert.ToBase64String(key), Version = version });
        });

        // Get key by version
        group.MapGet("/version/{version:int}", async (
            int version,
            IKeyRotationService keyRotationService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var key = await keyRotationService.GetKeyByVersionAsync(userId, version, ct);
            return Results.Ok(Convert.ToBase64String(key));
        });

        // Get all keys
        group.MapGet("/all", async (
            IKeyRotationService keyRotationService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var keys = await keyRotationService.GetAllKeysAsync(userId, ct);
            return Results.Ok(keys.ToDictionary(
                kvp => kvp.Key,
                kvp => Convert.ToBase64String(kvp.Value)));
        });

        // Revoke key
        group.MapDelete("/version/{version:int}", async (
            int version,
            IKeyRotationService keyRotationService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await keyRotationService.RevokeKeyAsync(userId, version, ct);
            return result ? Results.NoContent() : Results.NotFound();
        });

        // Create backup
        group.MapPost("/backup", async (
            [FromBody] CreateBackupRequest request,
            IKeyBackupService keyBackupService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var masterKey = Convert.FromBase64String(request.MasterKey);
            var backupData = await keyBackupService.CreateBackupAsync(userId, masterKey, ct);
            return Results.Ok(backupData);
        });

        // Restore backup
        group.MapPost("/backup/restore", async (
            [FromBody] RestoreBackupRequest request,
            IKeyBackupService keyBackupService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var masterKey = await keyBackupService.RestoreBackupAsync(userId, request.BackupData, ct);
            return Results.Ok(Convert.ToBase64String(masterKey));
        });

        // Verify backup
        group.MapPost("/backup/verify", async (
            [FromBody] VerifyBackupRequest request,
            IKeyBackupService keyBackupService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var isValid = await keyBackupService.VerifyBackupAsync(userId, request.BackupData, ct);
            return Results.Ok(isValid);
        });

        // Revoke backup
        group.MapDelete("/backup", async (
            IKeyBackupService keyBackupService,
            ClaimsPrincipal user,
            CancellationToken ct) =>
        {
            var userId = Guid.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value!);
            var result = await keyBackupService.RevokeBackupAsync(userId, ct);
            return result ? Results.NoContent() : Results.NotFound();
        });
    }
}

public record CreateBackupRequest(string MasterKey);
public record RestoreBackupRequest(string BackupData);
public record VerifyBackupRequest(string BackupData);
