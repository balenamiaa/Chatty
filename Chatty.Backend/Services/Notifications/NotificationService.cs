using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Infrastructure.Configuration;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Devices;
using Chatty.Shared.Models.Enums;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Chatty.Backend.Services.Notifications;

public sealed class NotificationService(
    ChattyDbContext context,
    ILogger<NotificationService> logger,
    IOptions<NotificationSettings> notificationSettings)
    : INotificationService
{
    public async Task<Result<bool>> SendToUserAsync(
        Guid userId,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default)
    {
        try
        {
            // Get user's devices
            var devices = await context.UserDevices
                .Where(d => d.UserId == userId)
                .ToListAsync(ct);

            if (!devices.Any())
            {
                return Result<bool>.Success(true); // No devices to send to
            }

            foreach (var device in devices)
            {
                if (string.IsNullOrEmpty(device.DeviceToken))
                {
                    continue;
                }

                // TODO: Implement actual push notification sending based on device type
                switch (device.DeviceType)
                {
                    case DeviceType.iOS:
                        // TODO: Send APNs notification
                        break;
                    case DeviceType.Android:
                        // TODO: Send FCM notification
                        break;
                    case DeviceType.Web:
                        // TODO: Send Web Push notification
                        break;
                }
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notification to user {UserId}", userId);
            return Result<bool>.Failure(Error.Internal("Failed to send notification"));
        }
    }

    public async Task<Result<bool>> SendToDeviceAsync(
        string deviceToken,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default)
    {
        try
        {
            var device = await context.UserDevices
                .FirstOrDefaultAsync(d => d.DeviceToken == deviceToken, ct);

            if (device is null)
            {
                return Result<bool>.Failure(Error.NotFound("Device not found"));
            }

            // Use notification settings for different providers
            switch (device.DeviceType)
            {
                case DeviceType.iOS:
                    // TODO: Send APNs notification
                    break;
                case DeviceType.Android:
                    // TODO: Send FCM notification
                    break;
                case DeviceType.Web:
                    // TODO: Send Web Push notification
                    break;
                case DeviceType.Desktop:
                    // TODO: Send Electron notification
                    break;
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notification to device {DeviceToken}", deviceToken);
            return Result<bool>.Failure(Error.Internal("Failed to send notification"));
        }
    }

    public async Task<Result<bool>> SendToDevicesAsync(
        IEnumerable<string> deviceTokens,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default)
    {
        try
        {
            var devices = await context.UserDevices
                .Where(d => deviceTokens.Contains(d.DeviceToken!))
                .ToListAsync(ct);

            // Group devices by type for batch sending
            var devicesByType = devices
                .Where(d => d.DeviceToken != null)
                .GroupBy(d => d.DeviceType);

            foreach (var group in devicesByType)
            {
                var tokens = group.Select(d => d.DeviceToken!).ToList();

                // TODO: Implement batch notification sending based on device type
                switch (group.Key)
                {
                    case DeviceType.iOS:
                        // TODO: Send batch APNs notifications
                        break;
                    case DeviceType.Android:
                        // TODO: Send batch FCM notifications
                        break;
                    case DeviceType.Web:
                        // TODO: Send batch Web Push notifications
                        break;
                    case DeviceType.Desktop:
                        // TODO: Send batch desktop notifications
                        break;
                }
            }

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to send notifications to multiple devices");
            return Result<bool>.Failure(Error.Internal("Failed to send notifications"));
        }
    }

    public async Task<Result<bool>> RegisterDeviceAsync(
        Guid userId,
        RegisterDeviceRequest request,
        CancellationToken ct = default)
    {
        try
        {
            var device = await context.UserDevices
                .FirstOrDefaultAsync(d =>
                    d.UserId == userId &&
                    d.DeviceId == request.DeviceId, ct);

            if (device is not null)
            {
                // Update existing device
                device.DeviceName = request.DeviceName;
                device.DeviceToken = request.DeviceToken;
                device.DeviceType = request.DeviceType;
                device.PublicKey = request.PublicKey;
                device.LastActiveAt = DateTime.UtcNow;
            }
            else
            {
                // Create new device
                device = new UserDevice
                {
                    UserId = userId,
                    DeviceId = request.DeviceId,
                    DeviceName = request.DeviceName,
                    DeviceToken = request.DeviceToken,
                    DeviceType = request.DeviceType,
                    PublicKey = request.PublicKey
                };
                context.UserDevices.Add(device);
            }

            await context.SaveChangesAsync(ct);
            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to register device for user {UserId}", userId);
            return Result<bool>.Failure(Error.Internal("Failed to register device"));
        }
    }

    public async Task<Result<bool>> UnregisterDeviceAsync(
        Guid userId,
        string deviceToken,
        CancellationToken ct = default)
    {
        try
        {
            var device = await context.UserDevices
                .FirstOrDefaultAsync(d =>
                    d.UserId == userId &&
                    d.DeviceToken == deviceToken, ct);

            if (device is null)
            {
                return Result<bool>.Success(true); // Already unregistered
            }

            context.UserDevices.Remove(device);
            await context.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to unregister device for user {UserId}", userId);
            return Result<bool>.Failure(Error.Internal("Failed to unregister device"));
        }
    }
}
