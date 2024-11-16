using Chatty.Shared.Models.Messages;

namespace Chatty.Backend.Data.Models.Extensions;

public static class MessageExtensions
{
    public static MessageDto ToDto(this Message message) => new(
        Id: message.Id,
        ChannelId: message.ChannelId,
        Sender: message.Sender!.ToDto(),
        Content: message.Content,
        ContentType: message.ContentType,
        SentAt: message.SentAt,
        UpdatedAt: message.UpdatedAt,
        IsDeleted: message.IsDeleted,
        MessageNonce: message.MessageNonce,
        KeyVersion: message.KeyVersion,
        ParentMessageId: message.ParentMessageId,
        ReplyCount: message.ReplyCount,
        Attachments: message.Attachments.Select(a => a.ToDto()).ToList(),
        Reactions: message.Reactions.Select(r => r.ToDto()).ToList());

    public static DirectMessageDto ToDto(this DirectMessage message) => new(
        Id: message.Id,
        Sender: message.Sender!.ToDto(),
        Recipient: message.Recipient!.ToDto(),
        Content: message.Content,
        ContentType: message.ContentType,
        SentAt: message.SentAt,
        IsDeleted: message.IsDeleted,
        MessageNonce: message.MessageNonce,
        KeyVersion: message.KeyVersion,
        ParentMessageId: message.ParentMessageId,
        ReplyCount: message.ReplyCount,
        Attachments: message.Attachments.Select(a => a.ToDto()).ToList(),
        Reactions: message.Reactions.Select(r => r.ToDto()).ToList());
}
