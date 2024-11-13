namespace Chatty.Shared.Models.Stickers;

public sealed record CreateStickerPackRequest(
    string Name,
    string? Description,
    string? ThumbnailUrl,
    bool IsPublished = false);