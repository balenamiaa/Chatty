using Chatty.Shared.Models.Enums;

namespace Chatty.Backend.Data.Models;

public sealed class Message
{
    public Guid Id { get; set; }
    public Guid ChannelId { get; set; }
    public Guid SenderId { get; set; }
    public required byte[] Content { get; set; }
    public required byte[] MessageNonce { get; set; }
    public ContentType ContentType { get; set; } = ContentType.Text;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public bool IsPinned { get; set; }
    public DateTime? PinnedAt { get; set; }
    public Guid? PinnedById { get; set; }
    public Guid? ReplyToId { get; set; }
    public int ReplyCount { get; set; }
    public string? Metadata { get; set; }
    public int KeyVersion { get; set; } = 1;

    public Guid? ParentMessageId { get; set; }

    // Navigation properties

    public Message? ParentMessage { get; set; }
    public Channel? Channel { get; set; }
    public User? Sender { get; set; }
    public User? PinnedBy { get; set; }
    public Message? ReplyTo { get; set; }
    public ICollection<Message> Replies { get; set; } = [];
    public ICollection<Attachment> Attachments { get; set; } = [];
    public ICollection<MessageReaction> Reactions { get; set; } = [];
    public ICollection<User> Mentions { get; set; } = [];
}
