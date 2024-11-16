using Chatty.Shared.Models.Enums;

namespace Chatty.Shared.Models.Attachments;

public sealed record AttachmentDto(
    Guid Id,
    string FileName,
    long FileSize,
    string ContentType,
    AttachmentType Type,
    string? ThumbnailUrl,
    byte[] EncryptionKey,
    byte[] EncryptionIv,
    DateTime CreatedAt);
