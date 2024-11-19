namespace Chatty.Shared.Realtime.Events;

public record MessageUnpinnedEvent(Guid ChannelId, Guid MessageId);
