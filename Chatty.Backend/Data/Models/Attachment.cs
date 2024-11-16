using Chatty.Shared.Models.Enums;

namespace Chatty.Backend.Data.Models;

public sealed class Attachment
{
    public Guid Id { get; set; }
    public Guid? MessageId { get; set; }
    public Guid? DirectMessageId { get; set; }
    public required string FileName { get; set; }
    public required long FileSize { get; set; }
    public required ContentType ContentType { get; set; }
    public required string StoragePath { get; set; }
    public string? ThumbnailPath { get; set; }
    public required byte[] EncryptionKey { get; set; }
    public required byte[] EncryptionIv { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public Message? Message { get; set; }
    public DirectMessage? DirectMessage { get; set; }
}
