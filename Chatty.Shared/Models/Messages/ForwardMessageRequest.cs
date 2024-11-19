namespace Chatty.Shared.Models.Messages;

/// <summary>
///     Request to forward a message to another channel
/// </summary>
public sealed record ForwardMessageRequest(
    Guid MessageId,
    Guid TargetChannelId);
