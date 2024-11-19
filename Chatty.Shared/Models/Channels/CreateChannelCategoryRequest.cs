namespace Chatty.Shared.Models.Channels;

/// <summary>
///     Request to create a channel category
/// </summary>
public record CreateChannelCategoryRequest(
    string Name,
    int? Position = null);
