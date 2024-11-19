using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Realtime.Events;
using Chatty.Backend.Services.Channels;
using Chatty.Backend.Services.Messages;
using Chatty.Backend.Tests.Helpers;
using Chatty.Shared.Crypto;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Realtime.Events;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using Moq;

using Xunit;

namespace Chatty.Backend.Tests.Services.Messages;

public sealed class MessageReactionTests : IDisposable
{
    private readonly Mock<IChannelService> _channelService = new();
    private readonly IDbContextFactory<ChattyDbContext> _contextFactory;
    private readonly Mock<IEventBus> _eventBus;
    private readonly MessageService _sut;

    public MessageReactionTests()
    {
        _contextFactory = TestDbContextFactory.CreateFactory();
        _eventBus = new Mock<IEventBus>();
        Mock<ICryptoProvider> crypto = new();
        Mock<ILogger<MessageService>> logger = new();
        var limitSettings = TestData.Settings.LimitSettings;

        _sut = new MessageService(
            _contextFactory,
            _eventBus.Object,
            crypto.Object,
            _channelService.Object,
            logger.Object,
            limitSettings);
    }

    public void Dispose()
    {
        using var context = _contextFactory.CreateDbContext();
        context.Database.EnsureDeleted();
    }

    [Fact]
    public async Task AddChannelMessageReactionAsync_WithValidRequest_AddsReactionAndPublishesEvent()
    {
        // Arrange
        await using var context = await _contextFactory.CreateDbContextAsync();
        var messageId = await CreateTestMessageAsync(context);
        var userId = TestData.Users.User2.Id;

        var message = await context.Messages.FirstOrDefaultAsync(x => x.Id == messageId);
        Assert.NotNull(message);

        // Act
        var result = await _sut.AddChannelMessageReactionAsync(messageId, userId, ReactionType.Like);

        // Assert
        Assert.True(result.IsSuccess);
        var reaction = result.Value;
        Assert.Equal(messageId, reaction.MessageId);
        Assert.Equal(userId, reaction.User.Id);
        Assert.Equal(ReactionType.Like, reaction.Type);

        // Verify event was published
        _eventBus.Verify(x => x.PublishAsync(
            It.Is<MessageReactionAddedEvent>(e =>
                e.ChannelId == message.ChannelId &&
                e.MessageId == messageId &&
                e.Reaction.Id == reaction.Id),
            default), Times.Once);
    }

    [Fact]
    public async Task AddDirectMessageReactionAsync_WithValidRequest_AddsReactionAndPublishesEvent()
    {
        // Arrange
        await using var context = await _contextFactory.CreateDbContextAsync();
        var message = await CreateTestDirectMessageAsync(context);
        var userId = message.RecipientId; // React as recipient

        // Act
        var result = await _sut.AddDirectMessageReactionAsync(message.Id, userId, ReactionType.Heart);

        // Assert
        Assert.True(result.IsSuccess);
        var reaction = result.Value;
        Assert.Equal(message.Id, reaction.MessageId);
        Assert.Equal(userId, reaction.User.Id);
        Assert.Equal(ReactionType.Heart, reaction.Type);

        // Verify event was published
        _eventBus.Verify(x => x.PublishAsync(
            It.Is<DirectMessageReactionAddedEvent>(e =>
                e.MessageId == message.Id &&
                e.Reaction.Id == reaction.Id),
            default), Times.Once);
    }

    [Fact]
    public async Task RemoveChannelMessageReactionAsync_WithValidRequest_RemovesReactionAndPublishesEvent()
    {
        // Arrange
        await using var context = await _contextFactory.CreateDbContextAsync();
        var messageId = await CreateTestMessageAsync(context);
        var userId = TestData.Users.User2.Id;

        var message = await context.Messages.FirstOrDefaultAsync(x => x.Id == messageId);
        Assert.NotNull(message);

        // Add reaction first
        var addResult = await _sut.AddChannelMessageReactionAsync(messageId, userId, ReactionType.Like);
        Assert.True(addResult.IsSuccess);
        var reactionId = addResult.Value.Id;

        // Act
        var result = await _sut.RemoveChannelMessageReactionAsync(messageId, reactionId, userId);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify reaction was removed
        await context.Entry(message).ReloadAsync();
        var reaction = await context.MessageReactions.FindAsync(reactionId);
        Assert.Null(reaction);

        // Verify event was published
        _eventBus.Verify(x => x.PublishAsync(
            It.Is<MessageReactionRemovedEvent>(e =>
                e.ChannelId == message.ChannelId &&
                e.MessageId == messageId &&
                e.ReactionId == reactionId &&
                e.UserId == userId),
            default), Times.Once);
    }

    [Fact]
    public async Task RemoveDirectMessageReactionAsync_WithValidRequest_RemovesReactionAndPublishesEvent()
    {
        // Arrange
        await using var context = await _contextFactory.CreateDbContextAsync();
        var message = await CreateTestDirectMessageAsync(context);
        var userId = message.RecipientId;

        // Add reaction first
        var addResult = await _sut.AddDirectMessageReactionAsync(message.Id, userId, ReactionType.Like);
        Assert.True(addResult.IsSuccess);
        var reactionId = addResult.Value.Id;

        // Act
        var result = await _sut.RemoveDirectMessageReactionAsync(message.Id, reactionId, userId);

        // Assert
        Assert.True(result.IsSuccess);

        // Verify reaction was removed
        await context.Entry(message).ReloadAsync();
        var reaction = await context.MessageReactions.FindAsync(reactionId);
        Assert.Null(reaction);

        // Verify event was published
        _eventBus.Verify(x => x.PublishAsync(
            It.Is<DirectMessageReactionRemovedEvent>(e =>
                e.MessageId == message.Id &&
                e.ReactionId == reactionId &&
                e.UserId == userId),
            default), Times.Once);
    }

    [Fact]
    public async Task AddChannelMessageReactionAsync_WithDuplicateReaction_ReturnsForbidden()
    {
        // Arrange
        await using var context = await _contextFactory.CreateDbContextAsync();
        var messageId = await CreateTestMessageAsync(context);
        var userId = TestData.Users.User2.Id;

        // Add first reaction
        await _sut.AddChannelMessageReactionAsync(messageId, userId, ReactionType.Like);

        // Act - Try to add same reaction again
        var result = await _sut.AddChannelMessageReactionAsync(messageId, userId, ReactionType.Like);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Already reacted with this reaction", result.Error.Message);
    }

    [Fact]
    public async Task AddChannelMessageReactionAsync_WithNonExistentMessage_ReturnsNotFound()
    {
        // Act
        var result = await _sut.AddChannelMessageReactionAsync(
            Guid.NewGuid(),
            TestData.Users.User1.Id,
            ReactionType.Like);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Message not found", result.Error.Message);
    }

    [Fact]
    public async Task AddChannelMessageReactionAsync_WithNonMemberUser_ReturnsForbidden()
    {
        // Arrange
        await using var context = await _contextFactory.CreateDbContextAsync();
        var messageId = await CreateTestMessageAsync(context);
        var userId = TestData.Users.User3.Id; // Not a member of the channel

        // Act
        var result = await _sut.AddChannelMessageReactionAsync(messageId, userId, ReactionType.Like);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Cannot react to this message", result.Error.Message);
    }

    [Fact]
    public async Task AddChannelMessageReactionAsync_WithCustomEmoji_AddsReactionAndPublishesEvent()
    {
        // Arrange
        await using var context = await _contextFactory.CreateDbContextAsync();
        var messageId = await CreateTestMessageAsync(context);
        var userId = TestData.Users.User2.Id;
        var customEmoji = "custom_emoji_1";

        var message = await context.Messages.FirstOrDefaultAsync(m => m.Id == messageId);
        Assert.NotNull(message);

        // Act
        var result = await _sut.AddChannelMessageReactionAsync(
            messageId,
            userId,
            ReactionType.Custom,
            customEmoji);

        // Assert
        Assert.True(result.IsSuccess);
        var reaction = result.Value;
        Assert.Equal(messageId, reaction.MessageId);
        Assert.Equal(userId, reaction.User.Id);
        Assert.Equal(ReactionType.Custom, reaction.Type);
        Assert.Equal(customEmoji, reaction.CustomEmoji);

        // Verify event was published
        _eventBus.Verify(x => x.PublishAsync(
            It.Is<MessageReactionAddedEvent>(e =>
                e.ChannelId == message.ChannelId &&
                e.MessageId == messageId &&
                e.Reaction.Id == reaction.Id &&
                e.Reaction.CustomEmoji == customEmoji),
            default), Times.Once);
    }

    [Fact]
    public async Task GetChannelMessageReactionsAsync_WithValidRequest_ReturnsReactions()
    {
        // Arrange
        await using var context = await _contextFactory.CreateDbContextAsync();
        var messageId = await CreateTestMessageAsync(context);

        // Add reactions from different users
        await _sut.AddChannelMessageReactionAsync(messageId, TestData.Users.User1.Id, ReactionType.Like);
        await _sut.AddChannelMessageReactionAsync(messageId, TestData.Users.User2.Id, ReactionType.Heart);

        // Act
        var result = await _sut.GetChannelMessageReactionsAsync(messageId);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, r => r.Type == ReactionType.Like);
        Assert.Contains(result.Value, r => r.Type == ReactionType.Heart);
    }

    [Fact]
    public async Task GetDirectMessageReactionsAsync_WithValidRequest_ReturnsReactions()
    {
        // Arrange
        await using var context = await _contextFactory.CreateDbContextAsync();
        var message = await CreateTestDirectMessageAsync(context);

        // Add reactions from both users
        await _sut.AddDirectMessageReactionAsync(message.Id, message.SenderId, ReactionType.Like);
        await _sut.AddDirectMessageReactionAsync(message.Id, message.RecipientId, ReactionType.Heart);

        // Act
        var result = await _sut.GetDirectMessageReactionsAsync(message.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value.Count);
        Assert.Contains(result.Value, r => r.Type == ReactionType.Like);
        Assert.Contains(result.Value, r => r.Type == ReactionType.Heart);
    }

    [Fact]
    public async Task AddChannelMessageReactionAsync_WithDeletedMessage_ReturnsForbidden()
    {
        // Arrange
        await using var context = await _contextFactory.CreateDbContextAsync();
        var messageId = await CreateTestMessageAsync(context);
        var userId = TestData.Users.User2.Id;

        // Get the message without query filters
        var message = await context.Messages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == messageId);
        Assert.NotNull(message);

        // Delete the message
        message.IsDeleted = true;
        await context.SaveChangesAsync();

        // Verify the message is marked as deleted
        var verifyMessage = await context.Messages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == messageId);
        Assert.NotNull(verifyMessage);
        Assert.True(verifyMessage.IsDeleted);

        // Act - Try to add reaction
        var result = await _sut.AddChannelMessageReactionAsync(messageId, userId, ReactionType.Like);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Cannot react to deleted messages", result.Error.Message);
    }

    [Fact]
    public async Task AddDirectMessageReactionAsync_WithDeletedMessage_ReturnsForbidden()
    {
        // Arrange
        await using var context = await _contextFactory.CreateDbContextAsync();
        var message = await CreateTestDirectMessageAsync(context);

        // Get the message without query filters
        var messageToUpdate = await context.DirectMessages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == message.Id);
        Assert.NotNull(messageToUpdate);

        // Delete the message
        messageToUpdate.IsDeleted = true;
        await context.SaveChangesAsync();

        // Verify the message is marked as deleted
        var verifyMessage = await context.DirectMessages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == message.Id);
        Assert.NotNull(verifyMessage);
        Assert.True(verifyMessage.IsDeleted);

        // Act - Try to add reaction
        var result = await _sut.AddDirectMessageReactionAsync(message.Id, message.RecipientId, ReactionType.Like);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Cannot react to deleted messages", result.Error.Message);
    }

    [Fact]
    public async Task AddChannelMessageReactionAsync_WithCustomEmojiButNoEmoji_ReturnsValidationError()
    {
        // Arrange
        await using var context = await _contextFactory.CreateDbContextAsync();
        var messageId = await CreateTestMessageAsync(context);
        var userId = TestData.Users.User2.Id;

        // Act
        var result = await _sut.AddChannelMessageReactionAsync(
            messageId,
            userId,
            ReactionType.Custom,
            null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Custom emoji is required for custom reactions", result.Error.Message);
    }

    [Fact]
    public async Task AddDirectMessageReactionAsync_WithCustomEmojiButNoEmoji_ReturnsValidationError()
    {
        // Arrange
        await using var context = await _contextFactory.CreateDbContextAsync();
        var message = await CreateTestDirectMessageAsync(context);
        var userId = message.RecipientId;

        // Act
        var result = await _sut.AddDirectMessageReactionAsync(
            message.Id,
            userId,
            ReactionType.Custom,
            null);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal("Custom emoji is required for custom reactions", result.Error.Message);
    }

    private static async Task<Guid> CreateTestMessageAsync(ChattyDbContext context)
    {
        // Create users if they don't exist
        var user1 = await context.Users.FindAsync(TestData.Users.User1.Id) ??
                    context.Users.Add(TestData.Users.User1).Entity;
        var user2 = await context.Users.FindAsync(TestData.Users.User2.Id) ??
                    context.Users.Add(TestData.Users.User2).Entity;
        await context.SaveChangesAsync();

        var channel = new Channel
        {
            Name = "test-channel",
            ChannelType = ChannelType.Text
        };

        context.Channels.Add(channel);
        await context.SaveChangesAsync();

        var member1 = new ChannelMember
        {
            ChannelId = channel.Id,
            UserId = user1.Id
        };

        var member2 = new ChannelMember
        {
            ChannelId = channel.Id,
            UserId = user2.Id
        };

        context.ChannelMembers.AddRange(member1, member2);
        await context.SaveChangesAsync();

        var message = new Message
        {
            ChannelId = channel.Id,
            SenderId = user1.Id,
            Content = [1, 2, 3],
            ContentType = ContentType.Text,
            MessageNonce = [4, 5, 6],
            KeyVersion = 1
        };

        context.Messages.Add(message);
        await context.SaveChangesAsync();

        return message.Id;
    }

    private static async Task<DirectMessage> CreateTestDirectMessageAsync(ChattyDbContext context)
    {
        // Create users if they don't exist
        var user1 = await context.Users.FindAsync(TestData.Users.User1.Id) ??
                    context.Users.Add(TestData.Users.User1).Entity;
        var user2 = await context.Users.FindAsync(TestData.Users.User2.Id) ??
                    context.Users.Add(TestData.Users.User2).Entity;
        await context.SaveChangesAsync();

        var message = new DirectMessage
        {
            SenderId = user1.Id,
            RecipientId = user2.Id,
            Content = [1, 2, 3],
            ContentType = ContentType.Text,
            MessageNonce = [4, 5, 6],
            KeyVersion = 1
        };

        context.DirectMessages.Add(message);
        await context.SaveChangesAsync();

        return message;
    }
}
