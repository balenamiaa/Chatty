namespace Chatty.Shared.Models.Notifications;

public sealed record SendNotificationRequest(
    string Title,
    string Body,
    Dictionary<string, string>? Data = null);

public sealed record SendMultiNotificationRequest(
    IEnumerable<string> DeviceTokens,
    string Title,
    string Body,
    Dictionary<string, string>? Data = null);