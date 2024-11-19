namespace Chatty.Shared.Models.Messages;

/// <summary>
///     Request to report a message for moderation
/// </summary>
public sealed record ReportMessageRequest(
    Guid MessageId,
    string Reason);
