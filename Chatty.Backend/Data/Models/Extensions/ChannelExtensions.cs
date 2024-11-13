using Chatty.Shared.Models.Channels;
using Chatty.Shared.Models.Messages;

namespace Chatty.Backend.Data.Models.Extensions;

public static class ChannelExtensions
{
    public static ChannelDto ToDto(this Channel channel) => new(
        Id: channel.Id,
        ServerId: channel.ServerId,
        Name: channel.Name,
        Topic: channel.Topic,
        IsPrivate: channel.IsPrivate,
        ChannelType: channel.ChannelType,
        Position: channel.Position,
        RateLimitPerUser: channel.RateLimitPerUser,
        CreatedAt: channel.CreatedAt,
        UpdatedAt: channel.UpdatedAt);
}