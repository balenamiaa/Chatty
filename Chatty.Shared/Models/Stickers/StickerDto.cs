namespace Chatty.Shared.Models.Stickers;

public sealed record StickerDto(
    Guid Id,
    string Name,
    string PackId,
    string AssetUrl,
    string? Description,
    string[]? Tags,
    bool IsCustom,
    Guid? CreatorId,
    DateTime CreatedAt);