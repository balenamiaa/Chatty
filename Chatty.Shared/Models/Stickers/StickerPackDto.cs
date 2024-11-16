namespace Chatty.Shared.Models.Stickers;

public sealed record StickerPackDto(
    string Id,
    string Name,
    string? Description,
    string? ThumbnailUrl,
    bool IsCustom,
    Guid? CreatorId,
    bool IsPublished,
    DateTime CreatedAt,
    DateTime? PublishedAt,
    IReadOnlyList<StickerDto> Stickers);
