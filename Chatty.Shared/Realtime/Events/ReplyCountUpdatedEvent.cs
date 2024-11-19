namespace Chatty.Shared.Realtime.Events;

public record ReplyCountUpdatedEvent(Guid MessageId, int ReplyCount);
