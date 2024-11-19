using Chatty.Shared.Models.Messages;

namespace Chatty.Shared.Realtime.Events;

public record MessageRepliedEvent(Guid MessageId, MessageDto Reply);
