using Chatty.Shared.Models.Messages;

namespace Chatty.Backend.Data.Models.Extensions;

public static class MessageExtensions
{
    public static MessageDto ToDto(this Message message) => new(
        message.Id,
        message.ChannelId,
        message.Sender!.ToDto(),
        message.Content,
        message.ContentType,
        message.SentAt,
        message.UpdatedAt,
        message.IsDeleted,
        message.MessageNonce,
        message.KeyVersion,
        message.ParentMessageId,
        message.ReplyCount,
        message.Attachments.Select(a => a.ToDto()).ToList(),
        message.Reactions.Select(r => r.ToDto()).ToList());

    public static DirectMessageDto ToDto(this DirectMessage message) => new(
        message.Id,
        message.Sender!.ToDto(),
        message.Recipient!.ToDto(),
        message.Content,
        message.ContentType,
        message.SentAt,
        message.IsDeleted,
        message.MessageNonce,
        message.KeyVersion,
        message.ParentMessageId,
        message.ReplyCount,
        message.Attachments.Select(a => a.ToDto()).ToList(),
        message.Reactions.Select(r => r.ToDto()).ToList());
}
