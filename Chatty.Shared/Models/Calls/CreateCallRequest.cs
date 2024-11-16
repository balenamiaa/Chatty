namespace Chatty.Shared.Models.Calls;

public sealed record CreateCallRequest(
    Guid? ChannelId,
    Guid? RecipientId);
