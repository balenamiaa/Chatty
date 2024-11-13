using Chatty.Backend.Services.Common;
using Chatty.Shared.Models.Attachments;
using Chatty.Shared.Models.Common;

namespace Chatty.Backend.Services.Files;

public interface IFileService : IService
{
    // All file operations are end-to-end encrypted
    Task<Result<AttachmentDto>> UploadAsync(Guid userId, CreateAttachmentRequest request, Stream content, CancellationToken ct = default);
    Task<Result<Stream>> DownloadAsync(Guid attachmentId, CancellationToken ct = default);
    Task<Result<bool>> DeleteAsync(Guid attachmentId, CancellationToken ct = default);
    Task<Result<string>> GetDownloadUrlAsync(Guid attachmentId, CancellationToken ct = default);
    Task<Result<string>> GetThumbnailUrlAsync(Guid attachmentId, CancellationToken ct = default);
}