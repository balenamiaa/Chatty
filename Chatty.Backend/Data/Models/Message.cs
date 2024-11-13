using Chatty.Shared.Models.Enums;

namespace Chatty.Backend.Data.Models;
public sealed class Message
{
    public Guid Id { get; set; }
    public Guid ChannelId { get; set; }
    public Guid SenderId { get; set; }
    public required byte[] Content { get; set; }
    public ContentType ContentType { get; set; } = ContentType.Text;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    public bool IsDeleted { get; set; }
    public required byte[] MessageNonce { get; set; }
    public int KeyVersion { get; set; } = 1;
    public Guid? ParentMessageId { get; set; }
    public int ReplyCount { get; set; }

    // Navigation properties
    public Channel Channel { get; set; } = null!;
    public User Sender { get; set; } = null!;
    public Message? ParentMessage { get; set; }
    public ICollection<Message> Replies { get; set; } = new List<Message>();
    public ICollection<Attachment> Attachments { get; set; } = new List<Attachment>();
}