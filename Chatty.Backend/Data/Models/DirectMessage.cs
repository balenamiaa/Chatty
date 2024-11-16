using Chatty.Shared.Models.Enums;

namespace Chatty.Backend.Data.Models;

public sealed class DirectMessage
{
    public Guid Id { get; set; }
    public Guid SenderId { get; set; }
    public User? Sender { get; set; }
    public Guid RecipientId { get; set; }
    public User? Recipient { get; set; }
    public required byte[] Content { get; set; }
    public ContentType ContentType { get; set; } = ContentType.Text;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public required byte[] MessageNonce { get; set; }
    public int KeyVersion { get; set; } = 1;
    public Guid? ParentMessageId { get; set; }
    public DirectMessage? ParentMessage { get; set; }
    public int ReplyCount { get; set; }
    public ICollection<DirectMessage> Replies { get; set; } = [];
    public ICollection<Attachment> Attachments { get; set; } = [];
    public ICollection<MessageReaction> Reactions { get; set; } = [];
}
