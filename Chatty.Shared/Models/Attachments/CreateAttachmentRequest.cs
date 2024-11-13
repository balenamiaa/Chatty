namespace Chatty.Shared.Models.Attachments;

public sealed record CreateAttachmentRequest(
    string FileName,
    long FileSize,
    string ContentType,
    byte[] EncryptionKey,
    byte[] EncryptionIv);