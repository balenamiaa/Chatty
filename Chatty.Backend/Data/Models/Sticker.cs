namespace Chatty.Backend.Data.Models;

public sealed class Sticker
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string PackId { get; set; }
    public required string AssetUrl { get; set; }
    public string? Description { get; set; }
    public string[]? Tags { get; set; }
    public bool IsCustom { get; set; }
    public Guid? CreatorId { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation properties
    public StickerPack Pack { get; set; } = null!;
    public User? Creator { get; set; }
}

public sealed class StickerPack
{
    public string Id { get; set; } = string.Empty;
    public required string Name { get; set; }
    public string? Description { get; set; }
    public string? ThumbnailUrl { get; set; }
    public bool IsCustom { get; set; }
    public Guid? CreatorId { get; set; }
    public bool IsPublished { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? PublishedAt { get; set; }

    // Navigation properties
    public User? Creator { get; set; }
    public ICollection<Sticker> Stickers { get; set; } = [];
    public ICollection<Server> EnabledServers { get; set; } = [];
}
