using System.Net;

namespace Chatty.Client.Exceptions;

/// <summary>
///     Exception thrown for file-related errors
/// </summary>
public class FileException : ChattyException
{
    public FileException(
        string message,
        string code,
        string operation,
        Exception? innerException = null,
        Guid? fileId = null,
        string? fileName = null,
        long? fileSize = null)
        : base(message, code, innerException)
    {
        FileId = fileId;
        FileName = fileName;
        FileSize = fileSize;
        Operation = operation;
    }

    public FileException(
        string message,
        string code,
        string operation,
        HttpStatusCode statusCode,
        Exception? innerException = null,
        Guid? fileId = null,
        string? fileName = null,
        long? fileSize = null)
        : base(message, code, statusCode, innerException)
    {
        FileId = fileId;
        FileName = fileName;
        FileSize = fileSize;
        Operation = operation;
    }

    /// <summary>
    ///     File ID if available
    /// </summary>
    public Guid? FileId { get; }

    /// <summary>
    ///     File name if available
    /// </summary>
    public string? FileName { get; }

    /// <summary>
    ///     File size in bytes if available
    /// </summary>
    public long? FileSize { get; }

    /// <summary>
    ///     Operation that failed
    /// </summary>
    public string Operation { get; }

    public static class ErrorCodes
    {
        public const string UploadFailed = "FILE_UPLOAD_FAILED";
        public const string DownloadFailed = "FILE_DOWNLOAD_FAILED";
        public const string DeleteFailed = "FILE_DELETE_FAILED";
        public const string NotFound = "FILE_NOT_FOUND";
        public const string InvalidFormat = "FILE_INVALID_FORMAT";
        public const string TooLarge = "FILE_TOO_LARGE";
        public const string EncryptionFailed = "FILE_ENCRYPTION_FAILED";
        public const string DecryptionFailed = "FILE_DECRYPTION_FAILED";
        public const string KeyNotFound = "FILE_KEY_NOT_FOUND";
        public const string InvalidKey = "FILE_INVALID_KEY";
        public const string AccessDenied = "FILE_ACCESS_DENIED";
        public const string QuotaExceeded = "FILE_QUOTA_EXCEEDED";
        public const string StorageFailed = "FILE_STORAGE_FAILED";
        public const string ValidationFailed = "FILE_VALIDATION_FAILED";
    }

    public static class Operations
    {
        public const string Upload = "UPLOAD";
        public const string Download = "DOWNLOAD";
        public const string Delete = "DELETE";
        public const string GetMetadata = "GET_METADATA";
        public const string Encrypt = "ENCRYPT";
        public const string Decrypt = "DECRYPT";
        public const string StoreKey = "STORE_KEY";
        public const string GetKey = "GET_KEY";
        public const string Validate = "VALIDATE";
    }
}
