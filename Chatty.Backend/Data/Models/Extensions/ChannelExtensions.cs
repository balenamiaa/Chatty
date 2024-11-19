using Chatty.Shared.Models.Channels;

namespace Chatty.Backend.Data.Models.Extensions;

public static class ChannelExtensions
{
    public static ChannelDto ToDto(this Channel channel) => new(
        channel.Id,
        channel.ServerId,
        channel.Name,
        channel.Topic,
        channel.IsPrivate,
        channel.ChannelType,
        channel.Position,
        channel.RateLimitPerUser,
        channel.CreatedAt,
        channel.UpdatedAt);
}
