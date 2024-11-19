using System.Collections.Concurrent;

using Chatty.Backend.Data;
using Chatty.Backend.Realtime;
using Chatty.Backend.Realtime.Events;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Realtime.Events;

using Microsoft.EntityFrameworkCore;

namespace Chatty.Backend.Services.Presence;

public sealed class PresenceService(
    IDbContextFactory<ChattyDbContext> contextFactory,
    IConnectionTracker connectionTracker,
    IEventBus eventBus,
    ILogger<PresenceService> logger)
    : IPresenceService
{
    private static readonly ConcurrentDictionary<Guid, (UserStatus Status, string? Message)> _userStatuses = new();

    public async Task<Result<bool>> UpdateStatusAsync(
        Guid userId,
        UserStatus status,
        string? statusMessage = null,
        CancellationToken ct = default)
    {
        try
        {
            // Update in-memory status
            _userStatuses.AddOrUpdate(
                userId,
                (status, statusMessage),
                (_, _) => (status, statusMessage));

            // Update database
            await using var context = await contextFactory.CreateDbContextAsync(ct);
            var user = await context.Users.FindAsync([userId], ct);
            if (user is null)
            {
                return Result<bool>.Failure(Error.NotFound("User not found"));
            }

            user.StatusMessage = statusMessage;
            user.UpdatedAt = DateTime.UtcNow;

            await context.SaveChangesAsync(ct);

            // Publish presence event to subscribers
            await eventBus.PublishAsync(
                new PresenceEvent(userId, status, statusMessage),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update status for user {UserId}", userId);
            return Result<bool>.Failure(Error.Internal("Failed to update status"));
        }
    }

    public async Task<Result<bool>> UpdateLastSeenAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        try
        {
            var isOnline = await connectionTracker.IsOnlineAsync(userId);

            // Create a new scope for database operations
            await using var context = await contextFactory.CreateDbContextAsync(ct);
            var user = await context.Users
                .FirstOrDefaultAsync(u => u.Id == userId, ct);

            if (user is null)
            {
                return Result<bool>.Failure(Error.NotFound("User not found"));
            }

            var previousLastSeen = user.LastOnlineAt;
            var wasOnline = previousLastSeen >= DateTime.UtcNow.AddMinutes(-5);

            // Always update LastOnlineAt if user is online
            if (isOnline)
            {
                user.LastOnlineAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;
                await context.SaveChangesAsync(ct);
            }

            // Only publish event if online state changed
            if (isOnline != wasOnline)
            {
                try
                {
                    await eventBus.PublishAsync(
                        new PresenceEvent(userId, isOnline ? UserStatus.Online : UserStatus.Offline,
                            user.StatusMessage),
                        ct);
                }
                catch (Exception ex)
                {
                    // Log but don't fail the operation if event publishing fails
                    logger.LogWarning(ex,
                        "Failed to publish presence events for user {UserId}. Operation will continue.",
                        userId);
                }
            }

            return Result<bool>.Success(true);
        }
        catch (ObjectDisposedException)
        {
            // Log and swallow ObjectDisposedException during cleanup
            logger.LogDebug(
                "Context disposed while updating last seen for user {UserId}. This is expected during cleanup.",
                userId);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to update last seen for user {UserId}", userId);
            return Result<bool>.Failure(Error.Internal("Failed to update last seen"));
        }
    }

    public async Task<Result<UserStatus>> GetUserStatusAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        try
        {
            if (_userStatuses.TryGetValue(userId, out var status))
            {
                return Result<UserStatus>.Success(status.Status);
            }

            var isOnline = await connectionTracker.IsOnlineAsync(userId);
            return Result<UserStatus>.Success(isOnline ? UserStatus.Online : UserStatus.Offline);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get status for user {UserId}", userId);
            return Result<UserStatus>.Failure(Error.Internal("Failed to get user status"));
        }
    }

    public async Task<Result<IReadOnlyDictionary<Guid, UserStatus>>> GetUsersStatusAsync(
        IEnumerable<Guid> userIds,
        CancellationToken ct = default)
    {
        try
        {
            var result = new Dictionary<Guid, UserStatus>();
            var usersToCheck = new List<Guid>();

            // Get cached statuses
            foreach (var userId in userIds)
            {
                if (_userStatuses.TryGetValue(userId, out var status))
                {
                    result[userId] = status.Status;
                }
                else
                {
                    usersToCheck.Add(userId);
                }
            }

            // Check online status for remaining users
            if (usersToCheck.Count > 0)
            {
                var connections = await connectionTracker.GetConnectionsAsync(usersToCheck);
                foreach (var userId in usersToCheck)
                {
                    result[userId] = connections[userId].Count > 0 ? UserStatus.Online : UserStatus.Offline;
                }
            }

            return Result<IReadOnlyDictionary<Guid, UserStatus>>.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to get status for multiple users");
            return Result<IReadOnlyDictionary<Guid, UserStatus>>.Failure(Error.Internal("Failed to get user statuses"));
        }
    }
}
