using Chatty.Backend.Data;

using Microsoft.EntityFrameworkCore;

namespace Chatty.Backend.Services.Background;

public sealed class FileCleanupService(
    IServiceScopeFactory scopeFactory,
    ILogger<FileCleanupService> logger)
    : BackgroundService
{
    private const string FileStoragePath = "uploads";
    private const string ThumbnailPath = "thumbnails";
    private readonly TimeSpan _interval = TimeSpan.FromHours(24);
    private readonly TimeSpan _unusedThreshold = TimeSpan.FromDays(7);

    protected async override Task ExecuteAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                await CleanupUnusedFilesAsync(ct);
                await Task.Delay(_interval, ct);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error occurred while cleaning up files");
                await Task.Delay(TimeSpan.FromMinutes(1), ct);
            }
        }
    }

    private async Task CleanupUnusedFilesAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ChattyDbContext>();

        var cutoffDate = DateTime.UtcNow - _unusedThreshold;

        // Get unused attachments
        var unusedAttachments = await context.Attachments
            .Where(a =>
                a.MessageId == null && a.DirectMessageId == null &&
                a.CreatedAt < cutoffDate)
            .ToListAsync(ct);

        if (!unusedAttachments.Any())
        {
            return;
        }

        var deletedCount = 0;
        foreach (var attachment in unusedAttachments)
        {
            try
            {
                // Delete file
                var filePath = Path.Combine(FileStoragePath, attachment.StoragePath);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }

                // Delete thumbnail if exists
                if (attachment.ThumbnailPath is not null)
                {
                    var thumbnailPath = Path.Combine(FileStoragePath, ThumbnailPath, attachment.ThumbnailPath);
                    if (File.Exists(thumbnailPath))
                    {
                        File.Delete(thumbnailPath);
                    }
                }

                context.Attachments.Remove(attachment);
                deletedCount++;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to delete attachment {AttachmentId}", attachment.Id);
            }
        }

        if (deletedCount > 0)
        {
            await context.SaveChangesAsync(ct);
            logger.LogInformation("Cleaned up {Count} unused attachments", deletedCount);
        }
    }
}
