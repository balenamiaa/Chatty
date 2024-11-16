using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Realtime.Events;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Stickers;
using Chatty.Shared.Realtime.Events;

using Microsoft.EntityFrameworkCore;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace Chatty.Backend.Services.Stickers;

public sealed class StickerService : IStickerService
{
    private const string StickerStoragePath = "stickers";
    private const int MaxStickerSize = 512;
    private readonly ChattyDbContext _context;
    private readonly ILogger<StickerService> _logger;
    private readonly IEventBus _eventBus;

    public StickerService(
        ChattyDbContext context,
        ILogger<StickerService> logger,
        IEventBus eventBus)
    {
        _context = context;
        _logger = logger;
        _eventBus = eventBus;

        // Ensure storage directory exists
        Directory.CreateDirectory(StickerStoragePath);
    }

    public async Task<Result<StickerPackDto>> CreatePackAsync(
        Guid userId,
        CreateStickerPackRequest request,
        CancellationToken ct = default)
    {
        var packId = GeneratePackId(request.Name);

        // Check if pack ID is already taken
        if (await _context.StickerPacks.AnyAsync(p => p.Id == packId, ct))
        {
            return Result<StickerPackDto>.Failure(Error.Conflict("A sticker pack with this name already exists"));
        }

        var pack = new StickerPack
        {
            Id = packId,
            Name = request.Name,
            Description = request.Description,
            ThumbnailUrl = request.ThumbnailUrl,
            IsCustom = true,
            CreatorId = userId,
            IsPublished = request.IsPublished
        };

        if (request.IsPublished)
        {
            pack.PublishedAt = DateTime.UtcNow;
        }

        _context.StickerPacks.Add(pack);
        await _context.SaveChangesAsync(ct);

        return Result<StickerPackDto>.Success(pack.ToDto());
    }

    public async Task<Result<StickerDto>> AddStickerAsync(
        string packId,
        Stream content,
        string name,
        string? description = null,
        string[]? tags = null,
        CancellationToken ct = default)
    {
        var pack = await _context.StickerPacks
            .Include(p => p.Creator)
            .FirstOrDefaultAsync(p => p.Id == packId, ct);

        if (pack is null)
            return Result<StickerDto>.Failure(Error.NotFound("Sticker pack not found"));

        try
        {
            // Process and save sticker image
            var fileName = $"{Guid.NewGuid()}.webp";
            var filePath = Path.Combine(StickerStoragePath, fileName);

            using (var image = await Image.LoadAsync(content, ct))
            {
                // Resize if needed
                if (image.Width > MaxStickerSize || image.Height > MaxStickerSize)
                {
                    var ratio = (float)MaxStickerSize / Math.Max(image.Width, image.Height);
                    var width = (int)(image.Width * ratio);
                    var height = (int)(image.Height * ratio);

                    image.Mutate(x => x.Resize(width, height));
                }

                await image.SaveAsWebpAsync(filePath, ct);
            }

            var sticker = new Sticker
            {
                Name = name,
                PackId = packId,
                AssetUrl = $"/stickers/{fileName}",
                Description = description,
                Tags = tags,
                IsCustom = true,
                CreatorId = pack.CreatorId
            };

            _context.Stickers.Add(sticker);
            await _context.SaveChangesAsync(ct);

            return Result<StickerDto>.Success(sticker.ToDto());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to add sticker to pack {PackId}", packId);
            return Result<StickerDto>.Failure(Error.Internal("Failed to add sticker"));
        }
    }

    public async Task<Result<bool>> DeleteStickerAsync(
        Guid stickerId,
        CancellationToken ct = default)
    {
        var sticker = await _context.Stickers
            .Include(s => s.Pack)
            .FirstOrDefaultAsync(s => s.Id == stickerId, ct);

        if (sticker is null)
            return Result<bool>.Success(true); // Already deleted

        try
        {
            // Delete sticker file
            if (!string.IsNullOrEmpty(sticker.AssetUrl))
            {
                var fileName = Path.GetFileName(sticker.AssetUrl);
                var filePath = Path.Combine(StickerStoragePath, fileName);
                if (File.Exists(filePath))
                {
                    File.Delete(filePath);
                }
            }

            _context.Stickers.Remove(sticker);
            await _context.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete sticker {StickerId}", stickerId);
            return Result<bool>.Failure(Error.Internal("Failed to delete sticker"));
        }
    }

    public async Task<Result<bool>> DeletePackAsync(
        string packId,
        CancellationToken ct = default)
    {
        var pack = await _context.StickerPacks
            .Include(p => p.Stickers)
            .FirstOrDefaultAsync(p => p.Id == packId, ct);

        if (pack is null)
            return Result<bool>.Success(true); // Already deleted

        try
        {
            // Delete all sticker files
            foreach (var sticker in pack.Stickers)
            {
                if (!string.IsNullOrEmpty(sticker.AssetUrl))
                {
                    var fileName = Path.GetFileName(sticker.AssetUrl);
                    var filePath = Path.Combine(StickerStoragePath, fileName);
                    if (File.Exists(filePath))
                    {
                        File.Delete(filePath);
                    }
                }
            }

            _context.StickerPacks.Remove(pack);
            await _context.SaveChangesAsync(ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete sticker pack {PackId}", packId);
            return Result<bool>.Failure(Error.Internal("Failed to delete sticker pack"));
        }
    }

    public async Task<Result<bool>> PublishPackAsync(
        string packId,
        CancellationToken ct = default)
    {
        var pack = await _context.StickerPacks
            .FirstOrDefaultAsync(p => p.Id == packId, ct);

        if (pack is null)
            return Result<bool>.Failure(Error.NotFound("Sticker pack not found"));

        try
        {
            pack.IsPublished = true;
            pack.PublishedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync(ct);

            // Publish sticker pack event
            await _eventBus.PublishAsync(
                new StickerPackPublishedEvent(pack.ToDto()),
                ct);

            return Result<bool>.Success(true);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to publish sticker pack");
            return Result<bool>.Failure(Error.Internal("Failed to publish sticker pack"));
        }
    }

    public async Task<Result<bool>> EnablePackForServerAsync(
        string packId,
        Guid serverId,
        CancellationToken ct = default)
    {
        var pack = await _context.StickerPacks
            .Include(p => p.EnabledServers)
            .FirstOrDefaultAsync(p => p.Id == packId, ct);

        if (pack is null)
            return Result<bool>.Failure(Error.NotFound("Sticker pack not found"));

        var server = await _context.Servers
            .FirstOrDefaultAsync(s => s.Id == serverId, ct);

        if (server is null)
            return Result<bool>.Failure(Error.NotFound("Server not found"));

        if (!pack.EnabledServers.Any(s => s.Id == serverId))
        {
            pack.EnabledServers.Add(server);
            await _context.SaveChangesAsync(ct);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<bool>> DisablePackForServerAsync(
        string packId,
        Guid serverId,
        CancellationToken ct = default)
    {
        var pack = await _context.StickerPacks
            .Include(p => p.EnabledServers)
            .FirstOrDefaultAsync(p => p.Id == packId, ct);

        if (pack is null)
            return Result<bool>.Failure(Error.NotFound("Sticker pack not found"));

        var server = pack.EnabledServers.FirstOrDefault(s => s.Id == serverId);
        if (server is not null)
        {
            pack.EnabledServers.Remove(server);
            await _context.SaveChangesAsync(ct);
        }

        return Result<bool>.Success(true);
    }

    public async Task<Result<IReadOnlyList<StickerPackDto>>> GetAvailablePacksAsync(
        Guid? serverId = null,
        CancellationToken ct = default)
    {
        var query = _context.StickerPacks
            .Include(p => p.Stickers)
            .Include(p => p.Creator)
            .Where(p => !p.IsCustom || p.IsPublished);

        if (serverId.HasValue)
        {
            query = query.Where(p =>
                !p.IsCustom ||
                p.EnabledServers.Any(s => s.Id == serverId.Value));
        }

        var packs = await query.ToListAsync(ct);

        return Result<IReadOnlyList<StickerPackDto>>.Success(
            packs.Select(p => p.ToDto()).ToList());
    }

    public async Task<Result<IReadOnlyList<StickerPackDto>>> GetUserPacksAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        var packs = await _context.StickerPacks
            .Include(p => p.Stickers)
            .Include(p => p.Creator)
            .Where(p => p.CreatorId == userId)
            .ToListAsync(ct);

        return Result<IReadOnlyList<StickerPackDto>>.Success(
            packs.Select(p => p.ToDto()).ToList());
    }

    private static string GeneratePackId(string name)
    {
        // Convert name to lowercase, replace spaces with underscores
        return name.ToLowerInvariant()
            .Replace(" ", "_")
            .Replace("-", "_")
            .Replace(".", "_")
            .Replace("'", "")
            .Replace("\"", "");
    }
}
