using System.Reactive.Linq;
using System.Reactive.Subjects;

using Chatty.Client.Cache;
using Chatty.Client.Crypto;
using Chatty.Client.Logging;
using Chatty.Client.Models;
using Chatty.Client.Storage;
using Chatty.Shared.Models.Calls;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Models.Servers;
using Chatty.Shared.Models.Users;

using Microsoft.Extensions.Logging;

namespace Chatty.Client.Realtime;

/// <summary>
///     Syncs local cache with realtime updates from SignalR
/// </summary>
public class RealtimeSyncService(
    IChattyRealtimeClient realtimeClient,
    ICacheService cache,
    ICryptoService cryptoService,
    IDeviceManager deviceManager,
    ILogger<RealtimeSyncService> logger)
    : IRealtimeSyncService, IDisposable
{
    private readonly Subject<(Guid MessageId, MessageReactionDto Reaction)> _directMessageReactionAdded = new();
    private readonly Subject<(Guid MessageId, Guid ReactionId, Guid UserId)> _directMessageReactionRemoved = new();
    private readonly Subject<(Guid ChannelId, MessageDto Message)> _messagePinned = new();

    private readonly Subject<(Guid ChannelId, Guid MessageId, MessageReactionDto Reaction)> _messageReactionAdded =
        new();

    private readonly Subject<(Guid ChannelId, Guid MessageId, Guid ReactionId, Guid UserId)> _messageReactionRemoved =
        new();

    private readonly List<IDisposable> _subscriptions = [];

    public IObservable<(Guid ChannelId, Guid MessageId, MessageReactionDto Reaction)> OnMessageReactionAdded =>
        _messageReactionAdded.AsObservable();

    public IObservable<(Guid ChannelId, Guid MessageId, Guid ReactionId, Guid UserId)> OnMessageReactionRemoved =>
        _messageReactionRemoved.AsObservable();

    public IObservable<(Guid MessageId, MessageReactionDto Reaction)> OnDirectMessageReactionAdded =>
        _directMessageReactionAdded.AsObservable();

    public IObservable<(Guid MessageId, Guid ReactionId, Guid UserId)> OnDirectMessageReactionRemoved =>
        _directMessageReactionRemoved.AsObservable();

    public IObservable<(Guid ChannelId, MessageDto Message)> OnMessagePinned => _messagePinned.AsObservable();

    public void Start()
    {
        logger.Info("Starting realtime sync service");

        // Handle new messages
        _subscriptions.Add(realtimeClient.OnMessageReceived.Subscribe(async message =>
        {
            try
            {
                // Decrypt message if needed
                if (message.ChannelId.HasValue)
                {
                    var key = await deviceManager.GetChannelKeyAsync(message.ChannelId.Value);
                    if (key is not null)
                    {
                        message = message with
                        {
                            Content = await cryptoService.DecryptMessageAsync(
                                message.Content,
                                message.ChannelId.Value,
                                message.KeyVersion)
                        };
                    }
                }

                // Update message cache
                await cache.SetAsync(
                    CacheKeys.Message(message.Id),
                    message);

                // Update channel messages cache
                if (message.ChannelId.HasValue)
                {
                    var messages = await cache.GetAsync<List<MessageDto>>(
                        CacheKeys.ChannelMessages(message.ChannelId.Value));

                    if (messages is not null)
                    {
                        messages.Insert(0, message);
                        await cache.SetAsync(
                            CacheKeys.ChannelMessages(message.ChannelId.Value),
                            messages);
                    }
                }

                // Update thread cache if it's a reply
                if (message.ParentMessageId.HasValue)
                {
                    var replies = await cache.GetAsync<List<MessageDto>>(
                        CacheKeys.MessageReplies(message.ParentMessageId.Value));

                    if (replies is not null)
                    {
                        replies.Add(message);
                        await cache.SetAsync(
                            CacheKeys.MessageReplies(message.ParentMessageId.Value),
                            replies);
                    }

                    // Update reply count
                    var replyCount = await cache.GetAsync<IntState>(
                        CacheKeys.MessageReplyCount(message.ParentMessageId.Value)) ?? new IntState();

                    await cache.SetAsync(
                        CacheKeys.MessageReplyCount(message.ParentMessageId.Value),
                        new IntState(replyCount.Value + 1));
                }

                logger.Debug("Processed new message",
                    ("MessageId", message.Id),
                    ("ChannelId", message.ChannelId),
                    ("ParentId", message.ParentMessageId));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process new message", ex,
                    ("MessageId", message.Id),
                    ("ChannelId", message.ChannelId));
            }
        }));

        // Handle message updates
        _subscriptions.Add(realtimeClient.OnMessageUpdated.Subscribe(async update =>
        {
            try
            {
                var (channelId, message) = update;

                // Decrypt message if needed
                var key = await deviceManager.GetChannelKeyAsync(channelId);
                if (key is not null)
                {
                    message = message with
                    {
                        Content = await cryptoService.DecryptMessageAsync(
                            message.Content,
                            channelId,
                            message.KeyVersion)
                    };
                }

                // Update message cache
                await cache.SetAsync(
                    CacheKeys.Message(message.Id),
                    message);

                // Update channel messages cache
                var messages = await cache.GetAsync<List<MessageDto>>(
                    CacheKeys.ChannelMessages(channelId));

                if (messages is not null)
                {
                    var index = messages.FindIndex(m => m.Id == message.Id);
                    if (index >= 0)
                    {
                        messages[index] = message;
                        await cache.SetAsync(
                            CacheKeys.ChannelMessages(channelId),
                            messages);
                    }
                }

                // Update thread cache if it's a reply
                if (message.ParentMessageId.HasValue)
                {
                    var replies = await cache.GetAsync<List<MessageDto>>(
                        CacheKeys.MessageReplies(message.ParentMessageId.Value));

                    if (replies is not null)
                    {
                        var replyIndex = replies.FindIndex(m => m.Id == message.Id);
                        if (replyIndex >= 0)
                        {
                            replies[replyIndex] = message;
                            await cache.SetAsync(
                                CacheKeys.MessageReplies(message.ParentMessageId.Value),
                                replies);
                        }
                    }
                }

                logger.Debug("Processed message update",
                    ("MessageId", message.Id),
                    ("ChannelId", channelId));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process message update", ex,
                    ("MessageId", update.Message.Id),
                    ("ChannelId", update.ChannelId));
            }
        }));

        // Handle message deletions
        _subscriptions.Add(realtimeClient.OnMessageDeleted.Subscribe(async deletion =>
        {
            try
            {
                var (channelId, messageId) = deletion;

                // Get message before removing
                var message = await cache.GetAsync<MessageDto>(
                    CacheKeys.Message(messageId));

                // Remove from message cache
                await cache.RemoveAsync(CacheKeys.Message(messageId));

                // Remove from channel messages cache
                var messages = await cache.GetAsync<List<MessageDto>>(
                    CacheKeys.ChannelMessages(channelId));

                if (messages is not null)
                {
                    messages.RemoveAll(m => m.Id == messageId);
                    await cache.SetAsync(
                        CacheKeys.ChannelMessages(channelId),
                        messages);
                }

                // Update thread cache if it was a reply
                if (message?.ParentMessageId.HasValue == true)
                {
                    var replies = await cache.GetAsync<List<MessageDto>>(
                        CacheKeys.MessageReplies(message.ParentMessageId.Value));

                    if (replies is not null)
                    {
                        replies.RemoveAll(m => m.Id == messageId);
                        await cache.SetAsync(
                            CacheKeys.MessageReplies(message.ParentMessageId.Value),
                            replies);
                    }

                    // Update reply count
                    var replyCount = await cache.GetAsync<IntState>(
                        CacheKeys.MessageReplyCount(message.ParentMessageId.Value)) ?? new IntState();

                    await cache.SetAsync(
                        CacheKeys.MessageReplyCount(message.ParentMessageId.Value),
                        new IntState(Math.Max(0, replyCount.Value - 1)));
                }

                logger.Debug("Processed message deletion",
                    ("MessageId", messageId),
                    ("ChannelId", channelId));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process message deletion", ex,
                    ("MessageId", deletion.MessageId),
                    ("ChannelId", deletion.ChannelId));
            }
        }));

        // Handle direct messages
        _subscriptions.Add(realtimeClient.OnDirectMessageReceived.Subscribe(async message =>
        {
            try
            {
                // TODO: Decrypt direct message when implemented

                // Update message cache
                await cache.SetAsync(
                    CacheKeys.Message(message.Id),
                    message);

                // Update direct messages cache
                var messages = await cache.GetAsync<List<DirectMessageDto>>(
                    CacheKeys.DirectMessages(message.Sender.Id, message.Recipient.Id));

                if (messages is not null)
                {
                    messages.Insert(0, message);
                    await cache.SetAsync(
                        CacheKeys.DirectMessages(message.Sender.Id, message.Recipient.Id),
                        messages);
                }

                logger.Debug("Processed new direct message",
                    ("MessageId", message.Id),
                    ("SenderId", message.Sender.Id));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process direct message", ex,
                    ("MessageId", message.Id),
                    ("SenderId", message.Sender.Id));
            }
        }));

        // Handle typing indicators
        _subscriptions.Add(realtimeClient.OnTypingStarted.Subscribe(async typing =>
        {
            try
            {
                var (channelId, user) = typing;

                // Update typing users cache
                var typingUsers = await cache.GetAsync<HashSet<UserDto>>(
                    CacheKeys.ChannelTypingUsers(channelId)) ?? [];

                typingUsers.Add(user);
                await cache.SetAsync(
                    CacheKeys.ChannelTypingUsers(channelId),
                    typingUsers,
                    TimeSpan.FromSeconds(30)); // Expire after 30 seconds

                logger.Debug("Added typing indicator",
                    ("ChannelId", channelId),
                    ("UserId", user.Id));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process typing started", ex,
                    ("ChannelId", typing.ChannelId),
                    ("UserId", typing.User.Id));
            }
        }));

        _subscriptions.Add(realtimeClient.OnTypingStopped.Subscribe(async typing =>
        {
            try
            {
                var (channelId, user) = typing;

                // Update typing users cache
                var typingUsers = await cache.GetAsync<HashSet<UserDto>>(
                    CacheKeys.ChannelTypingUsers(channelId));

                if (typingUsers is not null)
                {
                    typingUsers.RemoveWhere(u => u.Id == user.Id);
                    await cache.SetAsync(
                        CacheKeys.ChannelTypingUsers(channelId),
                        typingUsers,
                        TimeSpan.FromSeconds(30)); // Expire after 30 seconds
                }

                logger.Debug("Removed typing indicator",
                    ("ChannelId", channelId),
                    ("UserId", user.Id));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process typing stopped", ex,
                    ("ChannelId", typing.ChannelId),
                    ("UserId", typing.User.Id));
            }
        }));

        // Handle server membership
        _subscriptions.Add(realtimeClient.OnServerMemberJoined.Subscribe(async join =>
        {
            try
            {
                var (serverId, member) = join;

                // Update server members cache
                var members = await cache.GetAsync<List<ServerMemberDto>>(
                    CacheKeys.ServerMembers(serverId)) ?? [];

                members.Add(member);
                await cache.SetAsync(
                    CacheKeys.ServerMembers(serverId),
                    members);

                logger.Debug("Added server member",
                    ("ServerId", serverId),
                    ("UserId", member.User.Id));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process server member joined", ex,
                    ("ServerId", join.ServerId),
                    ("UserId", join.Member.User.Id));
            }
        }));

        _subscriptions.Add(realtimeClient.OnServerMemberLeft.Subscribe(async leave =>
        {
            try
            {
                var (serverId, user) = leave;

                // Update server members cache
                var members = await cache.GetAsync<List<ServerMemberDto>>(
                    CacheKeys.ServerMembers(serverId));

                if (members is not null)
                {
                    members.RemoveAll(m => m.User.Id == user.Id);
                    await cache.SetAsync(
                        CacheKeys.ServerMembers(serverId),
                        members);
                }

                logger.Debug("Removed server member",
                    ("ServerId", serverId),
                    ("UserId", user.Id));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process server member left", ex,
                    ("ServerId", leave.ServerId),
                    ("UserId", leave.User.Id));
            }
        }));

        // Handle user presence
        _subscriptions.Add(realtimeClient.OnUserPresenceChanged.Subscribe(async presence =>
        {
            try
            {
                var (userId, isOnline) = presence;

                // Update user presence cache
                await cache.SetAsync(
                    CacheKeys.UserPresence(userId),
                    new BooleanState(isOnline),
                    TimeSpan.FromMinutes(5)); // Expire after 5 minutes

                logger.Debug("Updated user presence",
                    ("UserId", userId),
                    ("IsOnline", isOnline));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process presence change", ex,
                    ("UserId", presence.UserId),
                    ("IsOnline", presence.IsOnline));
            }
        }));

        // Handle channel member events
        _subscriptions.Add(realtimeClient.OnChannelMemberJoined.Subscribe(async memberJoined =>
        {
            try
            {
                var (channelId, user) = memberJoined;

                // Update channel members cache
                var members = await cache.GetAsync<List<UserDto>>(
                    CacheKeys.ChannelMembers(channelId)) ?? [];

                if (!members.Any(m => m.Id == user.Id))
                {
                    members.Add(user);
                    await cache.SetAsync(
                        CacheKeys.ChannelMembers(channelId),
                        members);

                    logger.Debug("Added channel member",
                        ("ChannelId", channelId),
                        ("UserId", user.Id));
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process channel member joined", ex,
                    ("ChannelId", memberJoined.ChannelId),
                    ("UserId", memberJoined.User.Id));
            }
        }));

        _subscriptions.Add(realtimeClient.OnChannelMemberLeft.Subscribe(async memberLeft =>
        {
            try
            {
                var (channelId, userId) = memberLeft;

                // Update channel members cache
                var members = await cache.GetAsync<List<UserDto>>(
                    CacheKeys.ChannelMembers(channelId));

                if (members is not null)
                {
                    members.RemoveAll(m => m.Id == userId);
                    await cache.SetAsync(
                        CacheKeys.ChannelMembers(channelId),
                        members);

                    logger.Debug("Removed channel member",
                        ("ChannelId", channelId),
                        ("UserId", userId));
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process channel member left", ex,
                    ("ChannelId", memberLeft.ChannelId),
                    ("UserId", memberLeft.UserId));
            }
        }));

        _subscriptions.Add(realtimeClient.OnChannelMemberUpdated.Subscribe(async memberUpdated =>
        {
            try
            {
                var (channelId, user) = memberUpdated;

                // Update channel members cache
                var members = await cache.GetAsync<List<UserDto>>(
                    CacheKeys.ChannelMembers(channelId));

                if (members is not null)
                {
                    var index = members.FindIndex(m => m.Id == user.Id);
                    if (index >= 0)
                    {
                        members[index] = user;
                        await cache.SetAsync(
                            CacheKeys.ChannelMembers(channelId),
                            members);

                        logger.Debug("Updated channel member",
                            ("ChannelId", channelId),
                            ("UserId", user.Id));
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process channel member updated", ex,
                    ("ChannelId", memberUpdated.ChannelId),
                    ("UserId", memberUpdated.User.Id));
            }
        }));

        // Handle call participant events
        _subscriptions.Add(realtimeClient.OnCallParticipantJoined.Subscribe(async participant =>
        {
            try
            {
                var (callId, participantDto) = participant;

                // Update call participants cache
                var participants = await cache.GetAsync<HashSet<CallParticipantDto>>(
                    CacheKeys.CallParticipants(callId)) ?? [];

                participants.Add(participantDto);
                await cache.SetAsync(
                    CacheKeys.CallParticipants(callId),
                    participants);

                logger.Debug("Added call participant",
                    ("CallId", callId),
                    ("ParticipantId", participantDto.User?.Id));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process call participant joined", ex,
                    ("CallId", participant.CallId),
                    ("ParticipantId", participant.Participant.User?.Id));
            }
        }));

        _subscriptions.Add(realtimeClient.OnCallParticipantLeft.Subscribe(async participant =>
        {
            try
            {
                var (callId, participantDto) = participant;

                // Update call participants cache
                var participants = await cache.GetAsync<HashSet<CallParticipantDto>>(
                    CacheKeys.CallParticipants(callId));

                if (participants is not null)
                {
                    participants.RemoveWhere(p => p.User?.Id == participantDto.User?.Id);
                    await cache.SetAsync(
                        CacheKeys.CallParticipants(callId),
                        participants);
                }

                logger.Debug("Removed call participant",
                    ("CallId", callId),
                    ("ParticipantId", participantDto.User?.Id));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process call participant left", ex,
                    ("CallId", participant.CallId),
                    ("ParticipantId", participant.Participant.User?.Id));
            }
        }));

        // Handle user status changes
        _subscriptions.Add(realtimeClient.OnUserStatusChanged.Subscribe(async status =>
        {
            try
            {
                var (userId, userStatus) = status;
                await cache.SetAsync(
                    CacheKeys.UserStatus(userId),
                    new UserStatusState(userStatus),
                    TimeSpan.FromMinutes(5));

                logger.Debug("Updated user status",
                    ("UserId", userId),
                    ("Status", userStatus));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process user status change", ex,
                    ("UserId", status.UserId),
                    ("Status", status.Status));
            }
        }));

        // Handle user activity changes
        _subscriptions.Add(realtimeClient.OnUserActivityChanged.Subscribe(async activity =>
        {
            try
            {
                var (userId, activityText) = activity;
                await cache.SetAsync(
                    CacheKeys.UserActivity(userId),
                    activityText,
                    TimeSpan.FromMinutes(5));

                logger.Debug("Updated user activity",
                    ("UserId", userId),
                    ("Activity", activityText));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process user activity change", ex,
                    ("UserId", activity.UserId),
                    ("Activity", activity.Activity));
            }
        }));

        // Handle user streaming status
        _subscriptions.Add(realtimeClient.OnUserStreamingChanged.Subscribe(async streaming =>
        {
            try
            {
                var (userId, isStreaming) = streaming;
                await cache.SetAsync(
                    CacheKeys.UserStreaming(userId),
                    new BooleanState(isStreaming),
                    TimeSpan.FromMinutes(5));

                logger.Debug("Updated user streaming status",
                    ("UserId", userId),
                    ("IsStreaming", isStreaming));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process user streaming change", ex,
                    ("UserId", streaming.UserId),
                    ("IsStreaming", streaming.IsStreaming));
            }
        }));

        // Handle direct messages
        _subscriptions.Add(realtimeClient.OnDirectMessageReceived.Subscribe(async message =>
        {
            try
            {
                // Update direct message cache
                await cache.SetAsync(
                    CacheKeys.DirectMessage(message.Id),
                    message);

                // Update conversation messages cache
                var messages = await cache.GetAsync<List<DirectMessageDto>>(
                    CacheKeys.DirectMessages(message.Sender.Id, message.Recipient.Id)) ?? [];

                messages.Insert(0, message);
                await cache.SetAsync(
                    CacheKeys.DirectMessages(message.Sender.Id, message.Recipient.Id),
                    messages);

                logger.Debug("Processed new direct message",
                    ("MessageId", message.Id),
                    ("SenderId", message.Sender.Id),
                    ("ReceiverId", message.Recipient.Id));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process new direct message", ex,
                    ("MessageId", message.Id));
            }
        }));

        // Handle typing indicators
        _subscriptions.Add(realtimeClient.OnTypingStarted.Subscribe(async typing =>
        {
            try
            {
                var (channelId, user) = typing;
                var typingUsers = await cache.GetAsync<HashSet<UserDto>>(
                    CacheKeys.ChannelTypingUsers(channelId)) ?? [];

                typingUsers.Add(user);
                await cache.SetAsync(
                    CacheKeys.ChannelTypingUsers(channelId),
                    typingUsers,
                    TimeSpan.FromSeconds(30)); // Expire after 30 seconds

                logger.Debug("User started typing",
                    ("ChannelId", channelId),
                    ("UserId", user.Id));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process typing started", ex,
                    ("ChannelId", typing.ChannelId),
                    ("UserId", typing.User.Id));
            }
        }));

        _subscriptions.Add(realtimeClient.OnTypingStopped.Subscribe(async typing =>
        {
            try
            {
                var (channelId, user) = typing;
                var typingUsers = await cache.GetAsync<HashSet<UserDto>>(
                    CacheKeys.ChannelTypingUsers(channelId));

                if (typingUsers != null)
                {
                    typingUsers.RemoveWhere(u => u.Id == user.Id);
                    await cache.SetAsync(
                        CacheKeys.ChannelTypingUsers(channelId),
                        typingUsers,
                        TimeSpan.FromSeconds(30));
                }

                logger.Debug("User stopped typing",
                    ("ChannelId", channelId),
                    ("UserId", user.Id));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process typing stopped", ex,
                    ("ChannelId", typing.ChannelId),
                    ("UserId", typing.User.Id));
            }
        }));

        // Handle call events
        _subscriptions.Add(realtimeClient.OnCallStarted.Subscribe(async call =>
        {
            try
            {
                var (callId, callData) = call;
                await cache.SetAsync(
                    CacheKeys.Call(callId),
                    callData);

                logger.Debug("Call started",
                    ("CallId", callId),
                    ("ChannelId", callData.ChannelId));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process call started", ex,
                    ("CallId", call.CallId));
            }
        }));

        _subscriptions.Add(realtimeClient.OnCallParticipantJoined.Subscribe(async participant =>
        {
            try
            {
                var (callId, participantData) = participant;
                var call = await cache.GetAsync<CallDto>(CacheKeys.Call(callId));

                if (call != null)
                {
                    var participants = call.Participants.ToList();
                    participants.Add(participantData);
                    var updatedCall = call with { Participants = participants };

                    await cache.SetAsync(
                        CacheKeys.Call(callId),
                        updatedCall);
                }

                logger.Debug("Call participant joined",
                    ("CallId", callId),
                    ("UserId", participantData?.User?.Id));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process call participant joined", ex,
                    ("CallId", participant.CallId),
                    ("UserId", participant.Participant?.User?.Id));
            }
        }));

        _subscriptions.Add(realtimeClient.OnCallParticipantLeft.Subscribe(async participant =>
        {
            try
            {
                var (callId, participantData) = participant;
                var call = await cache.GetAsync<CallDto>(CacheKeys.Call(callId));

                if (call != null)
                {
                    var participants = call.Participants.ToList();
                    participants.RemoveAll(p => p.User?.Id == participantData.User?.Id);
                    var updatedCall = call with { Participants = participants };

                    await cache.SetAsync(
                        CacheKeys.Call(callId),
                        updatedCall);
                }

                logger.Debug("Call participant left",
                    ("CallId", callId),
                    ("UserId", participantData.User?.Id));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process call participant left", ex,
                    ("CallId", participant.CallId),
                    ("UserId", participant.Participant.User?.Id));
            }
        }));

        _subscriptions.Add(realtimeClient.OnCallParticipantMuted.Subscribe(async participant =>
        {
            try
            {
                var (callId, participantData, isMuted) = participant;
                var call = await cache.GetAsync<CallDto>(CacheKeys.Call(callId));

                if (call != null)
                {
                    var participants = call.Participants.ToList();
                    var index = participants.FindIndex(p => p.User?.Id == participantData.User?.Id);
                    if (index >= 0)
                    {
                        participants[index] = participantData;
                        var updatedCall = call with { Participants = participants };
                        await cache.SetAsync(
                            CacheKeys.Call(callId),
                            updatedCall);
                    }
                }

                logger.Debug("Call participant mute status changed",
                    ("CallId", callId),
                    ("UserId", participantData.User?.Id),
                    ("IsMuted", isMuted));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process call participant mute change", ex,
                    ("CallId", participant.CallId),
                    ("UserId", participant.Participant.User?.Id));
            }
        }));

        _subscriptions.Add(realtimeClient.OnScreenShareChanged.Subscribe(async share =>
        {
            try
            {
                var (callId, participant, isSharing) = share;
                var call = await cache.GetAsync<CallDto>(CacheKeys.Call(callId));

                if (call != null)
                {
                    var participants = call.Participants.ToList();
                    var index = participants.FindIndex(p => p.User?.Id == participant.User?.Id);
                    if (index >= 0)
                    {
                        participants[index] = participant;
                        var updatedCall = call with { Participants = participants };
                        await cache.SetAsync(
                            CacheKeys.Call(callId),
                            updatedCall);
                    }
                }

                logger.Debug("Screen share status changed",
                    ("CallId", callId),
                    ("UserId", participant.User?.Id),
                    ("IsSharing", isSharing));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process screen share change", ex,
                    ("CallId", share.CallId),
                    ("UserId", share.Participant.User?.Id));
            }
        }));

        // Handle reaction events
        _subscriptions.Add(realtimeClient.OnMessageReactionAdded.Subscribe(async reaction =>
        {
            try
            {
                var (channelId, messageId, reactionDto) = reaction;

                // Update message reactions cache
                var reactions = await cache.GetAsync<List<MessageReactionDto>>(
                    CacheKeys.MessageReactions(messageId)) ?? [];

                reactions.Add(reactionDto);
                await cache.SetAsync(
                    CacheKeys.MessageReactions(messageId),
                    reactions);

                logger.Debug("Added message reaction",
                    ("MessageId", messageId),
                    ("ReactionId", reactionDto.Id));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process message reaction added", ex,
                    ("MessageId", reaction.MessageId),
                    ("ReactionId", reaction.Reaction.Id));
            }
        }));

        _subscriptions.Add(realtimeClient.OnMessageReactionRemoved.Subscribe(async reaction =>
        {
            try
            {
                var (channelId, messageId, reactionId, userId) = reaction;

                // Update message reactions cache
                var reactions = await cache.GetAsync<List<MessageReactionDto>>(
                    CacheKeys.MessageReactions(messageId));

                if (reactions is not null)
                {
                    reactions.RemoveAll(r => r.Id == reactionId);
                    await cache.SetAsync(
                        CacheKeys.MessageReactions(messageId),
                        reactions);
                }

                logger.Debug("Removed message reaction",
                    ("MessageId", messageId),
                    ("ReactionId", reactionId));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process message reaction removed", ex,
                    ("MessageId", reaction.MessageId),
                    ("ReactionId", reaction.ReactionId));
            }
        }));

        _subscriptions.Add(realtimeClient.OnDirectMessageReactionAdded.Subscribe(async reaction =>
        {
            try
            {
                var (messageId, reactionDto) = reaction;

                // Update direct message reactions cache
                var reactions = await cache.GetAsync<List<MessageReactionDto>>(
                    CacheKeys.DirectMessageReactions(messageId)) ?? [];

                reactions.Add(reactionDto);
                await cache.SetAsync(
                    CacheKeys.DirectMessageReactions(messageId),
                    reactions);

                logger.Debug("Added direct message reaction",
                    ("MessageId", messageId),
                    ("ReactionId", reactionDto.Id));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process direct message reaction added", ex,
                    ("MessageId", reaction.MessageId),
                    ("ReactionId", reaction.Reaction.Id));
            }
        }));

        _subscriptions.Add(realtimeClient.OnDirectMessageReactionRemoved.Subscribe(async reaction =>
        {
            try
            {
                var (messageId, reactionId, userId) = reaction;

                // Update direct message reactions cache
                var reactions = await cache.GetAsync<List<MessageReactionDto>>(
                    CacheKeys.DirectMessageReactions(messageId));

                if (reactions is not null)
                {
                    reactions.RemoveAll(r => r.Id == reactionId);
                    await cache.SetAsync(
                        CacheKeys.DirectMessageReactions(messageId),
                        reactions);
                }

                logger.Debug("Removed direct message reaction",
                    ("MessageId", messageId),
                    ("ReactionId", reactionId));
            }
            catch (Exception ex)
            {
                logger.Error("Failed to process direct message reaction removed", ex,
                    ("MessageId", reaction.MessageId),
                    ("ReactionId", reaction.ReactionId));
            }
        }));

        logger.Info("Started realtime sync service");
    }

    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
        {
            subscription.Dispose();
        }

        logger.Info("Disposed realtime sync service");
    }
}
