using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Shared.Models.Attachments;
using Chatty.Shared.Models.Common;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;
using Chatty.Backend.Data.Models.Extensions;
using Chatty.Backend.Infrastructure.Configuration;
using Chatty.Shared.Models.Enums;
using Microsoft.Extensions.Options;
using System.Text;

namespace Chatty.Backend.Services.Files;

public sealed class FileService : IFileService
{
    private readonly ChattyDbContext _context;
    private readonly ILogger<FileService> _logger;
    private readonly StorageSettings _storageSettings;
    private readonly string _basePath;
    private readonly string _thumbnailPath;

    public FileService(
        ChattyDbContext context,
        ILogger<FileService> logger,
        IOptions<StorageSettings> storageSettings)
    {
        _context = context;
        _logger = logger;
        _storageSettings = storageSettings.Value;

        _basePath = storageSettings.Value.BasePath;
        _thumbnailPath = Path.Combine(_basePath, "thumbnails");

        // Ensure storage directories exist
        Directory.CreateDirectory(_basePath);
        Directory.CreateDirectory(_thumbnailPath);
    }

    public async Task<Result<AttachmentDto>> UploadAsync(
        Guid userId,
        CreateAttachmentRequest request,
        Stream content,
        CancellationToken ct = default)
    {
        // Validate file size
        if (request.FileSize > _storageSettings.MaxFileSize)
            return Result<AttachmentDto>.Failure(
                Error.Validation($"File size exceeds maximum of {_storageSettings.MaxFileSize} bytes"));

        // Validate content type
        if (!_storageSettings.AllowedFileTypes.Contains(request.ContentType))
            return Result<AttachmentDto>.Failure(Error.Validation("File type not allowed"));

        if (!await ValidateFileTypeAsync(content, request.ContentType))
            return Result<AttachmentDto>.Failure(Error.Validation("Invalid file type"));

        try
        {
            // Generate unique filename
            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(request.FileName)}";
            var filePath = Path.Combine(_storageSettings.BasePath, fileName);

            // Save encrypted file
            await using (var fileStream = File.Create(filePath))
            {
                await content.CopyToAsync(fileStream, ct);
            }

            string? thumbnailPath = null;
            if (IsImage(request.ContentType))
            {
                thumbnailPath = await GenerateThumbnailAsync(filePath, fileName, ct);
            }

            // Create attachment record
            var attachment = new Attachment
            {
                FileName = request.FileName,
                FileSize = request.FileSize,
                ContentType = DetermineContentType(request.ContentType),
                StoragePath = fileName,
                ThumbnailPath = thumbnailPath,
                EncryptionKey = request.EncryptionKey,
                EncryptionIv = request.EncryptionIv
            };

            _context.Attachments.Add(attachment);
            await _context.SaveChangesAsync(ct);

            return Result<AttachmentDto>.Success(attachment.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to upload file {FileName}", request.FileName);
            return Result<AttachmentDto>.Failure(Error.Internal("Failed to upload file"));
        }
    }

    public async Task<Result<Stream>> DownloadAsync(
        Guid attachmentId,
        CancellationToken ct = default)
    {
        var attachment = await _context.Attachments.FindAsync([attachmentId], ct);
        if (attachment is null)
            return Result<Stream>.Failure(Error.NotFound("Attachment not found"));

        var filePath = Path.Combine(_storageSettings.BasePath, attachment.StoragePath);
        if (!File.Exists(filePath))
            return Result<Stream>.Failure(Error.NotFound("File not found"));

        try
        {
            var stream = File.OpenRead(filePath);
            return Result<Stream>.Success(stream);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download file {FileName}", attachment.FileName);
            return Result<Stream>.Failure(Error.Internal("Failed to download file"));
        }
    }

    public async Task<Result<bool>> DeleteAsync(
        Guid attachmentId,
        CancellationToken ct = default)
    {
        var attachment = await _context.Attachments.FindAsync([attachmentId], ct);
        if (attachment is null)
            return Result<bool>.Success(true); // Already deleted

        try
        {
            // Delete file
            var filePath = Path.Combine(_storageSettings.BasePath, attachment.StoragePath);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            // Delete thumbnail if exists
            if (attachment.ThumbnailPath is not null)
            {
                var thumbnailPath = Path.Combine(_thumbnailPath, attachment.ThumbnailPath);
                if (File.Exists(thumbnailPath))
                {
                    File.Delete(thumbnailPath);
                }
            }

            _context.Attachments.Remove(attachment);
            await _context.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file {FileName}", attachment.FileName);
            return Result<bool>.Failure(Error.Internal("Failed to delete file"));
        }
    }

    public Task<Result<string>> GetDownloadUrlAsync(
        Guid attachmentId,
        CancellationToken ct = default)
    {
        // Generate temporary download URL
        var url = $"/api/v1/files/{attachmentId}/download";
        return Task.FromResult(Result<string>.Success(url));
    }

    public Task<Result<string>> GetThumbnailUrlAsync(
        Guid attachmentId,
        CancellationToken ct = default)
    {
        // Generate temporary thumbnail URL
        var url = $"/api/v1/files/{attachmentId}/thumbnail";
        return Task.FromResult(Result<string>.Success(url));
    }

    private static bool IsImage(string contentType)
    {
        return DetermineContentType(contentType) == ContentType.Image;
    }

    private async Task<string?> GenerateThumbnailAsync(
        string filePath,
        string fileName,
        CancellationToken ct)
    {
        try
        {
            var thumbnailFileName = $"thumb_{fileName}";
            var thumbnailPath = Path.Combine(_thumbnailPath, thumbnailFileName);

            using var image = await Image.LoadAsync(filePath, ct);
            var ratio = (float)_storageSettings.ThumbnailSize / Math.Max(image.Width, image.Height);
            var width = (int)(image.Width * ratio);
            var height = (int)(image.Height * ratio);

            image.Mutate(x => x.Resize(width, height));
            await image.SaveAsync(thumbnailPath, ct);

            return thumbnailFileName;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to generate thumbnail for {FileName}", fileName);
            return null;
        }
    }

    private static ContentType DetermineContentType(string mimeType)
    {
        return mimeType.ToLowerInvariant() switch
        {
            var mt when mt.StartsWith("image/") => ContentType.Image,
            var mt when mt.StartsWith("video/") => ContentType.Video,
            var mt when mt.StartsWith("audio/") => ContentType.Audio,
            "application/pdf" => ContentType.Document,
            "text/plain" => ContentType.Text,
            _ => ContentType.File
        };
    }

    private async Task<bool> ValidateFileTypeAsync(Stream content, string contentType)
    {
        // Read first few bytes to verify file signature
        var buffer = new byte[8];
        await content.ReadExactlyAsync(buffer);
        content.Position = 0;

        return contentType.ToLower() switch
        {
            "image/jpeg" => buffer[0] == 0xFF && buffer[1] == 0xD8,
            "image/png" => buffer[0] == 0x89 && buffer[1] == 0x50,
            "application/pdf" => Encoding.ASCII.GetString(buffer).StartsWith("%PDF"),
            _ => true // Allow other types
        };
    }
}