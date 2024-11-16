using Chatty.Shared.Models.Enums;

namespace Chatty.Backend.Data.Models;

public class MessageReaction
{
    public Guid Id { get; set; }

    // Channel message relationship
    public Guid? ChannelMessageId { get; set; }
    public Message? Message { get; set; }

    // Direct message relationship
    public Guid? DirectMessageId { get; set; }
    public DirectMessage? DirectMessage { get; set; }

    // The user who added the reaction
    public Guid UserId { get; set; }
    public User? User { get; set; }

    // The reaction type and optional custom emoji data
    public ReactionType Type { get; set; }
    public string? CustomEmoji { get; set; }

    // Metadata
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
