using Chatty.Backend.Data;
using Chatty.Backend.Realtime;
using Chatty.Backend.Realtime.Events;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Realtime.Events;

using Microsoft.EntityFrameworkCore;

namespace Chatty.Backend.Services.Background;

public sealed class PresenceUpdateService(
    IServiceScopeFactory scopeFactory,
    IConnectionTracker connectionTracker,
    IEventBus eventBus,
    ILogger<PresenceUpdateService> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromSeconds(30);
    private readonly TimeSpan _offlineThreshold = TimeSpan.FromMinutes(5);

    protected async override Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await UpdatePresenceStatesAsync(ct);
                await Task.Delay(_interval, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error occurred while updating presence states");
                await Task.Delay(TimeSpan.FromSeconds(5), ct);
            }
        }
    }

    private async Task UpdatePresenceStatesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ChattyDbContext>();

        var cutoffTime = DateTime.UtcNow - _offlineThreshold;

        // Get users who haven't been seen recently
        var inactiveUsers = await context.Users
            .Where(u => u.LastOnlineAt < cutoffTime)
            .ToListAsync(ct);

        foreach (var user in inactiveUsers)
        {
            // Check if user is actually offline
            var isOnline = await connectionTracker.IsOnlineAsync(user.Id);
            if (!isOnline)
            {
                // Update last seen time
                user.LastOnlineAt = DateTime.UtcNow;
                user.UpdatedAt = DateTime.UtcNow;

                // Publish offline event
                await eventBus.PublishAsync(
                    new PresenceEvent(user.Id, UserStatus.Offline, user.StatusMessage),
                    ct);
            }
        }

        if (inactiveUsers.Any())
        {
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Updated presence state for {Count} users", inactiveUsers.Count);
        }
    }
}
