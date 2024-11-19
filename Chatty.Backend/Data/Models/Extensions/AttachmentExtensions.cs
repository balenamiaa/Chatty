using Chatty.Shared.Models.Attachments;
using Chatty.Shared.Models.Enums;

namespace Chatty.Backend.Data.Models.Extensions;

public static class AttachmentExtensions
{
    public static AttachmentDto ToDto(this Attachment attachment) => new(
        attachment.Id,
        attachment.FileName,
        attachment.FileSize,
        attachment.ContentType.ToString(),
        GetAttachmentType(attachment.ContentType),
        attachment.ThumbnailPath != null ? $"/attachments/{attachment.Id}/thumbnail" : null,
        attachment.EncryptionKey,
        attachment.EncryptionIv,
        attachment.CreatedAt);

    private static AttachmentType GetAttachmentType(ContentType contentType) => contentType switch
    {
        ContentType.Image => AttachmentType.Image,
        ContentType.Video => AttachmentType.Video,
        ContentType.Audio => AttachmentType.Audio,
        ContentType.Document => AttachmentType.Document,
        _ => AttachmentType.Other
    };
}
