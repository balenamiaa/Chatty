using Chatty.Shared.Models.Stickers;

namespace Chatty.Shared.Realtime.Events;

public sealed record StickerPackPublishedEvent(StickerPackDto Pack);