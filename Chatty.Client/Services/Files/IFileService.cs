using Chatty.Shared.Models.Attachments;

namespace Chatty.Client.Services;

/// <summary>
///     Service for managing file uploads and downloads
/// </summary>
public interface IFileService
{
    /// <summary>
    ///     Gets a file by ID
    /// </summary>
    Task<AttachmentDto> GetAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>
    ///     Uploads a file
    /// </summary>
    Task<AttachmentDto> UploadAsync(
        Stream content,
        string fileName,
        string contentType,
        CancellationToken ct = default);

    /// <summary>
    ///     Downloads a file
    /// </summary>
    Task<Stream> DownloadAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>
    ///     Deletes a file
    /// </summary>
    Task DeleteAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>
    ///     Gets the URL for a file
    /// </summary>
    Task<string> GetUrlAsync(Guid fileId, CancellationToken ct = default);

    /// <summary>
    ///     Gets the thumbnail URL for a file
    /// </summary>
    Task<string> GetThumbnailUrlAsync(Guid fileId, CancellationToken ct = default);

    // TODO: Add methods for:
    // - File encryption/decryption
    // - File metadata
    // - File sharing
    // - File versioning
    // - File search
    // - File quotas
}
