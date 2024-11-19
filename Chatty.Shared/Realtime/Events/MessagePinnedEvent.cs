using Chatty.Shared.Models.Messages;

namespace Chatty.Shared.Realtime.Events;

public record MessagePinnedEvent(Guid ChannelId, MessageDto Message);
