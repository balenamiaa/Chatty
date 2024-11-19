namespace Chatty.Shared.Models.Channels;

/// <summary>
///     Request to update a channel category
/// </summary>
public record UpdateChannelCategoryRequest(
    string? Name = null,
    int? Position = null);
