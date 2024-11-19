using Chatty.Backend.Services.Common;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Stickers;

namespace Chatty.Backend.Services.Stickers;

public interface IStickerService : IService
{
    Task<Result<StickerPackDto>> CreatePackAsync(Guid userId, CreateStickerPackRequest request,
        CancellationToken ct = default);

    Task<Result<StickerDto>> AddStickerAsync(string packId, Stream content, string name, string? description = null,
        string[]? tags = null, CancellationToken ct = default);

    Task<Result<bool>> DeleteStickerAsync(Guid stickerId, CancellationToken ct = default);
    Task<Result<bool>> DeletePackAsync(string packId, CancellationToken ct = default);
    Task<Result<bool>> PublishPackAsync(string packId, CancellationToken ct = default);
    Task<Result<bool>> EnablePackForServerAsync(string packId, Guid serverId, CancellationToken ct = default);
    Task<Result<bool>> DisablePackForServerAsync(string packId, Guid serverId, CancellationToken ct = default);

    Task<Result<IReadOnlyList<StickerPackDto>>> GetAvailablePacksAsync(Guid? serverId = null,
        CancellationToken ct = default);

    Task<Result<IReadOnlyList<StickerPackDto>>> GetUserPacksAsync(Guid userId, CancellationToken ct = default);
}
