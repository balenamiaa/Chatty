namespace Chatty.Backend.Infrastructure.Configuration;

public sealed class NotificationSettings
{
    public required ApnsSettings Apns { get; init; }
    public required FcmSettings Fcm { get; init; }
    public required WebPushSettings WebPush { get; init; }
}

public sealed class ApnsSettings
{
    public required string BundleId { get; init; }
    public required string KeyId { get; init; }
    public required string TeamId { get; init; }
    public required string PrivateKeyPath { get; init; }
}

public sealed class FcmSettings
{
    public required string ProjectId { get; init; }
    public required string PrivateKeyPath { get; init; }
}

public sealed class WebPushSettings
{
    public required string PublicKey { get; init; }
    public required string PrivateKey { get; init; }
    public required string Subject { get; init; }
}