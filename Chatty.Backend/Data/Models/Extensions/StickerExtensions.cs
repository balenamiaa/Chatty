using Chatty.Shared.Models.Stickers;

namespace Chatty.Backend.Data.Models;

public static class StickerExtensions
{
    public static StickerDto ToDto(this Sticker sticker) => new(
        Id: sticker.Id,
        Name: sticker.Name,
        PackId: sticker.PackId,
        AssetUrl: sticker.AssetUrl,
        Description: sticker.Description,
        Tags: sticker.Tags,
        IsCustom: sticker.IsCustom,
        CreatorId: sticker.CreatorId,
        CreatedAt: sticker.CreatedAt);

    public static StickerPackDto ToDto(this StickerPack pack) => new(
        Id: pack.Id,
        Name: pack.Name,
        Description: pack.Description,
        ThumbnailUrl: pack.ThumbnailUrl,
        IsCustom: pack.IsCustom,
        CreatorId: pack.CreatorId,
        IsPublished: pack.IsPublished,
        CreatedAt: pack.CreatedAt,
        PublishedAt: pack.PublishedAt,
        Stickers: pack.Stickers.Select(s => s.ToDto()).ToList());
}
