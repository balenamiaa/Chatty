using Chatty.Shared.Models.Stickers;

namespace Chatty.Backend.Data.Models;

public static class StickerExtensions
{
    public static StickerDto ToDto(this Sticker sticker) => new(
        sticker.Id,
        sticker.Name,
        sticker.PackId,
        sticker.AssetUrl,
        sticker.Description,
        sticker.Tags,
        sticker.IsCustom,
        sticker.CreatorId,
        sticker.CreatedAt);

    public static StickerPackDto ToDto(this StickerPack pack) => new(
        pack.Id,
        pack.Name,
        pack.Description,
        pack.ThumbnailUrl,
        pack.IsCustom,
        pack.CreatorId,
        pack.IsPublished,
        pack.CreatedAt,
        pack.PublishedAt,
        pack.Stickers.Select(s => s.ToDto()).ToList());
}
