using Chatty.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace Chatty.Backend.Services.Background;

public sealed class MessageCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<MessageCleanupService> logger)
    : BackgroundService
{
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);
    private readonly TimeSpan _deleteAfter = TimeSpan.FromDays(30);

    protected override async Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await CleanupMessagesAsync(ct);
                await Task.Delay(_interval, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error occurred while cleaning up messages");
                await Task.Delay(TimeSpan.FromMinutes(1), ct);
            }
        }
    }

    private async Task CleanupMessagesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ChattyDbContext>();

        var cutoffDate = DateTime.UtcNow - _deleteAfter;

        // Delete channel messages
        var deletedMessages = await context.Messages
            .Where(m => m.IsDeleted && m.UpdatedAt < cutoffDate)
            .ExecuteDeleteAsync(ct);

        // Delete direct messages
        var deletedDirectMessages = await context.DirectMessages
            .Where(m => m.IsDeleted && m.SentAt < cutoffDate)
            .ExecuteDeleteAsync(ct);

        if (deletedMessages > 0 || deletedDirectMessages > 0)
        {
            logger.LogInformation(
                "Cleaned up {MessageCount} messages and {DirectMessageCount} direct messages",
                deletedMessages, deletedDirectMessages);
        }
    }
}