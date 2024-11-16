using Chatty.Shared.Models.Attachments;
using Chatty.Shared.Models.Enums;

namespace Chatty.Backend.Data.Models.Extensions;

public static class AttachmentExtensions
{
    public static AttachmentDto ToDto(this Attachment attachment) => new(
        Id: attachment.Id,
        FileName: attachment.FileName,
        FileSize: attachment.FileSize,
        ContentType: attachment.ContentType.ToString(),
        Type: GetAttachmentType(attachment.ContentType),
        ThumbnailUrl: attachment.ThumbnailPath != null ? $"/attachments/{attachment.Id}/thumbnail" : null,
        EncryptionKey: attachment.EncryptionKey,
        EncryptionIv: attachment.EncryptionIv,
        CreatedAt: attachment.CreatedAt);

    private static AttachmentType GetAttachmentType(ContentType contentType) => contentType switch
    {
        ContentType.Image => AttachmentType.Image,
        ContentType.Video => AttachmentType.Video,
        ContentType.Audio => AttachmentType.Audio,
        ContentType.Document => AttachmentType.Document,
        _ => AttachmentType.Other
    };
}
