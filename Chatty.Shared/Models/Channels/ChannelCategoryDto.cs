namespace Chatty.Shared.Models.Channels;

/// <summary>
///     Represents a channel category
/// </summary>
public record ChannelCategoryDto(
    Guid Id,
    Guid ServerId,
    string Name,
    int Position,
    IReadOnlyList<ChannelDto> Channels);
