namespace Chatty.Backend.Infrastructure.Configuration;

public sealed class StorageSettings
{
    public required string BasePath { get; init; }
    public long MaxFileSize { get; init; } = 104857600; // 100MB
    public required string[] AllowedFileTypes { get; init; }
}