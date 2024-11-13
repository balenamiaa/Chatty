using Chatty.Backend.Services.Common;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Devices;

namespace Chatty.Backend.Services.Notifications;

public interface INotificationService : IService
{
    Task<Result<bool>> SendToUserAsync(
        Guid userId,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default);

    Task<Result<bool>> SendToDeviceAsync(
        string deviceToken,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default);

    Task<Result<bool>> SendToDevicesAsync(
        IEnumerable<string> deviceTokens,
        string title,
        string body,
        Dictionary<string, string>? data = null,
        CancellationToken ct = default);

    Task<Result<bool>> RegisterDeviceAsync(
        Guid userId,
        RegisterDeviceRequest request,
        CancellationToken ct = default);

    Task<Result<bool>> UnregisterDeviceAsync(
        Guid userId,
        string deviceToken,
        CancellationToken ct = default);
}