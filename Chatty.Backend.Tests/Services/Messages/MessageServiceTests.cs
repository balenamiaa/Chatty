using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Infrastructure.Configuration;
using Chatty.Backend.Realtime.Events;
using Chatty.Backend.Services.Messages;
using Chatty.Backend.Tests.Helpers;
using Chatty.Shared.Crypto;
using Chatty.Shared.Models.Common;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Realtime.Events;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using Xunit;

namespace Chatty.Backend.Tests.Services.Messages;

public sealed class MessageServiceTests : IDisposable
{
    private readonly IDbContextFactory<ChattyDbContext> _contextFactory;
    private readonly Mock<IEventBus> _eventBus;
    private readonly MessageService _sut;

    public MessageServiceTests()
    {
        _contextFactory = TestDbContextFactory.CreateFactory();
        _eventBus = new Mock<IEventBus>();
        Mock<ICryptoProvider> crypto = new();
        Mock<ILogger<MessageService>> logger = new();

        _sut = new MessageService(
            _contextFactory,
            _eventBus.Object,
            crypto.Object,
            logger.Object,
            TestData.Settings.LimitSettings);

        SetupTestData().Wait();
    }

    private async Task SetupTestData()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Add users
        await context.Users.AddAsync(TestData.Users.User1);
        await context.Users.AddAsync(TestData.Users.User2);

        // Add channel
        await context.Channels.AddAsync(TestData.Channels.TextChannel);

        // Add channel membership for User1 only (User2 is not a member)
        await context.ChannelMembers.AddAsync(new ChannelMember
        {
            ChannelId = TestData.Channels.TextChannel.Id,
            UserId = TestData.Users.User1.Id
        });

        await context.SaveChangesAsync();
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var userId = TestData.Users.User1.Id;
        var request = new CreateMessageRequest(
            ChannelId: TestData.Channels.TextChannel.Id,
            Content: [1, 2, 3],
            ContentType: ContentType.Text,
            MessageNonce: [4, 5, 6],
            KeyVersion: 1);

        // Act
        var result = await _sut.CreateAsync(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(request.Content, result.Value.Content);
        Assert.Equal(request.ContentType, result.Value.ContentType);
        Assert.Equal(request.MessageNonce, result.Value.MessageNonce);
        Assert.Equal(request.KeyVersion, result.Value.KeyVersion);

        // Verify event was published
        _eventBus.Verify(x => x.PublishAsync(
                It.Is<MessageEvent>(e =>
                    e.ChannelId == request.ChannelId &&
                    e.Message.Id == result.Value.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidChannel_ReturnsFailure()
    {
        // Arrange
        var userId = TestData.Users.User1.Id;
        var request = new CreateMessageRequest(
            ChannelId: Guid.NewGuid(), // Non-existent channel
            Content: [1, 2, 3],
            ContentType: ContentType.Text,
            MessageNonce: [4, 5, 6],
            KeyVersion: 1);

        // Act
        var result = await _sut.CreateAsync(userId, request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("NotFound", result.Error.Code);
    }

    [Fact]
    public async Task CreateAsync_WithNonMember_ReturnsFailure()
    {
        // Arrange
        var userId = TestData.Users.User2.Id; // User2 is not a member of Channel1
        var request = new CreateMessageRequest(
            ChannelId: TestData.Channels.TextChannel.Id,
            Content: [1, 2, 3],
            ContentType: ContentType.Text,
            MessageNonce: [4, 5, 6],
            KeyVersion: 1);

        // Act
        var result = await _sut.CreateAsync(userId, request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Forbidden", result.Error.Code);
    }

    [Fact]
    public async Task GetChannelMessagesAsync_WithPagination_ReturnsCorrectOrder()
    {

        await using var context = await _contextFactory.CreateDbContextAsync();

        // Arrange
        var channelId = TestData.Channels.TextChannel.Id;
        var messages = new List<Message>();

        for (int i = 0; i < 10; i++)
        {
            messages.Add(new Message
            {
                Id = Guid.NewGuid(),
                ChannelId = channelId,
                SenderId = TestData.Users.User1.Id,
                Content = [1, 2, 3],
                ContentType = ContentType.Text,
                MessageNonce = [4, 5, 6],
                KeyVersion = 1,
                SentAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }

        context.Messages.AddRange(messages);
        await context.SaveChangesAsync();

        // Act
        var result = await _sut.GetChannelMessagesAsync(channelId, 5);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Count);
        Assert.True(result.Value[0].SentAt > result.Value[1].SentAt); // Descending order
    }

    [Fact]
    public async Task CreateAsync_WithAttachments_HandlesAttachmentsCorrectly()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Arrange
        var userId = TestData.Users.User1.Id;
        var attachments = new[]
        {
            new Attachment
            {
                Id = Guid.NewGuid(),
                FileName = "test.txt",
                FileSize = 100,
                ContentType = ContentType.Text,
                StoragePath = "test/path",
                EncryptionKey = [1, 2, 3],
                EncryptionIv = [4, 5, 6]
            }
        };

        context.Attachments.AddRange(attachments);
        await context.SaveChangesAsync();

        var request = new CreateMessageRequest(
            ChannelId: TestData.Channels.TextChannel.Id,
            Content: [1, 2, 3],
            ContentType: ContentType.Text,
            MessageNonce: [4, 5, 6],
            KeyVersion: 1,
            Attachments: attachments.Select(a => a.Id).ToList());

        // Act
        var result = await _sut.CreateAsync(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(attachments.Length, result.Value.Attachments.Count);
        Assert.Equal(attachments[0].Id, result.Value.Attachments[0].Id);
    }

    [Fact]
    public async Task CreateAsync_ExceedingRateLimit_ReturnsFailure()
    {
        // Arrange
        var userId = TestData.Users.User1.Id;
        var request = new CreateMessageRequest(
            ChannelId: TestData.Channels.TextChannel.Id,
            Content: [1, 2, 3],
            ContentType: ContentType.Text,
            MessageNonce: [4, 5, 6],
            KeyVersion: 1);

        // Send messages until rate limit is exceeded
        for (int i = 0; i < TestData.Settings.LimitSettings.Value.RateLimits.Messages.Points; i++)
        {
            await _sut.CreateAsync(userId, request);
        }

        // Act
        var result = await _sut.CreateAsync(userId, request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("TooManyRequests", result.Error.Code);
    }

    [Fact]
    public async Task DeleteAsync_WithAttachments_CleansUpAttachments()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Arrange
        var message = await CreateMessageWithAttachment(context);

        // Act
        var result = await _sut.DeleteAsync(message.Id, message.SenderId);

        // Assert
        Assert.True(result.IsSuccess);
        var attachments = await context.Attachments
            .Where(a => a.MessageId == message.Id)
            .ToListAsync();
        Assert.Empty(attachments);
    }

    private async Task<Message> CreateMessageWithAttachment(ChattyDbContext context)
    {
        var attachment = new Attachment
        {
            Id = Guid.NewGuid(),
            FileName = "test.txt",
            FileSize = 100,
            ContentType = ContentType.Text,
            StoragePath = "test/path",
            EncryptionKey = [1, 2, 3],
            EncryptionIv = [4, 5, 6]
        };

        var message = new Message
        {
            Id = Guid.NewGuid(),
            ChannelId = TestData.Channels.TextChannel.Id,
            SenderId = TestData.Users.User1.Id,
            Content = [1, 2, 3],
            ContentType = ContentType.Text,
            MessageNonce = [4, 5, 6],
            KeyVersion = 1,
            Attachments = [attachment]
        };

        context.Messages.Add(message);
        await context.SaveChangesAsync();
        return message;
    }

    [Fact]
    public async Task CreateDirectAsync_WithValidRequest_ReturnsSuccess()
    {
        // Arrange
        var request = new CreateDirectMessageRequest(
            RecipientId: TestData.Users.User2.Id,
            Content: [1, 2, 3],
            ContentType: ContentType.Text,
            MessageNonce: [4, 5, 6],
            KeyVersion: 1);

        // Act
        var result = await _sut.CreateDirectAsync(TestData.Users.User1.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(request.Content, result.Value.Content);
        Assert.Equal(request.ContentType, result.Value.ContentType);
        Assert.Equal(request.MessageNonce, result.Value.MessageNonce);
        Assert.Equal(request.KeyVersion, result.Value.KeyVersion);
        Assert.Equal(TestData.Users.User1.Id, result.Value.Sender.Id);
        Assert.Equal(TestData.Users.User2.Id, result.Value.Recipient.Id);

        // Verify event was published
        _eventBus.Verify(x => x.PublishAsync(
            It.Is<DirectMessageEvent>(e =>
                e.Message.Sender.Id == TestData.Users.User1.Id &&
                e.Message.Recipient.Id == TestData.Users.User2.Id &&
                e.Message.Content.SequenceEqual(request.Content)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateDirectAsync_WithBlockedContact_ReturnsFailure()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Arrange
        var contact = new Contact
        {
            UserId = TestData.Users.User1.Id,
            ContactUserId = TestData.Users.User2.Id,
            Status = ContactStatus.Blocked
        };
        context.Contacts.Add(contact);
        await context.SaveChangesAsync();

        var request = new CreateDirectMessageRequest(
            RecipientId: TestData.Users.User2.Id,
            Content: [1, 2, 3],
            ContentType: ContentType.Text,
            MessageNonce: [4, 5, 6],
            KeyVersion: 1);

        // Act
        var result = await _sut.CreateDirectAsync(TestData.Users.User1.Id, request);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Forbidden", result.Error.Code);
        Assert.Equal("Cannot message blocked contact", result.Error.Message);
    }

    [Fact]
    public async Task DeleteDirectAsync_WithValidRequest_PublishesEvent()
    {
        // Arrange
        var message = new DirectMessage
        {
            Id = Guid.NewGuid(),
            SenderId = TestData.Users.User1.Id,
            RecipientId = TestData.Users.User2.Id,
            Content = [1, 2, 3],
            ContentType = ContentType.Text,
            MessageNonce = [4, 5, 6],
            KeyVersion = 1
        };

        await using (var context1 = await _contextFactory.CreateDbContextAsync())
        {
            context1.DirectMessages.Add(message);
            await context1.SaveChangesAsync();

            // Act
            var result = await _sut.DeleteDirectAsync(message.Id, TestData.Users.User1.Id);

            // Assert
            Assert.True(result.IsSuccess);

            // Verify event was published
            _eventBus.Verify(x => x.PublishAsync(
                It.Is<DirectMessageDeletedEvent>(e => e.MessageId == message.Id),
                It.IsAny<CancellationToken>()),
                Times.Once);
        }

        await using var context2 = await _contextFactory.CreateDbContextAsync();

        // Verify message is marked as deleted
        var deletedMessage = await context2.DirectMessages
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(m => m.Id == message.Id);
        Assert.NotNull(deletedMessage);
        Assert.True(deletedMessage.IsDeleted);
    }

    [Fact]
    public async Task DeleteDirectAsync_WithOtherUserMessage_ReturnsForbidden()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Arrange
        var message = new DirectMessage
        {
            Id = Guid.NewGuid(),
            SenderId = TestData.Users.User2.Id,
            RecipientId = TestData.Users.User1.Id,
            Content = [1, 2, 3],
            ContentType = ContentType.Text,
            MessageNonce = [4, 5, 6],
            KeyVersion = 1
        };
        context.DirectMessages.Add(message);
        await context.SaveChangesAsync();

        // Act
        var result = await _sut.DeleteDirectAsync(message.Id, TestData.Users.User1.Id);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Forbidden", result.Error.Code);
        Assert.Equal("Cannot delete another user's message", result.Error.Message);
    }

    [Fact]
    public async Task GetDirectMessagesAsync_WithPagination_ReturnsCorrectOrder()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Arrange
        var messages = new List<DirectMessage>();
        for (int i = 0; i < 10; i++)
        {
            messages.Add(new DirectMessage
            {
                Id = Guid.NewGuid(),
                SenderId = TestData.Users.User1.Id,
                RecipientId = TestData.Users.User2.Id,
                Content = [1, 2, 3],
                ContentType = ContentType.Text,
                MessageNonce = [4, 5, 6],
                KeyVersion = 1,
                SentAt = DateTime.UtcNow.AddMinutes(-i)
            });
        }
        context.DirectMessages.AddRange(messages);
        await context.SaveChangesAsync();

        // Act
        var result = await _sut.GetDirectMessagesAsync(
            TestData.Users.User1.Id,
            TestData.Users.User2.Id,
            5);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(5, result.Value.Count);
        Assert.True(result.Value[0].SentAt > result.Value[1].SentAt); // Descending order
    }

    [Fact]
    public async Task UpdateDirectAsync_WithValidRequest_PublishesEvent()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        // Arrange
        var message = new DirectMessage
        {
            Id = Guid.NewGuid(),
            SenderId = TestData.Users.User1.Id,
            RecipientId = TestData.Users.User2.Id,
            Content = [1, 2, 3],
            ContentType = ContentType.Text,
            MessageNonce = [4, 5, 6],
            KeyVersion = 1
        };
        context.DirectMessages.Add(message);
        await context.SaveChangesAsync();

        var request = new UpdateDirectMessageRequest(
            Content: [7, 8, 9],
            ContentType: ContentType.Text,
            MessageNonce: [10, 11, 12],
            KeyVersion: 2);

        // Act
        var result = await _sut.UpdateDirectAsync(message.Id, TestData.Users.User1.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(request.Content, result.Value.Content);
        Assert.Equal(request.MessageNonce, result.Value.MessageNonce);
        Assert.Equal(request.KeyVersion, result.Value.KeyVersion);

        // Verify event was published
        _eventBus.Verify(x => x.PublishAsync(
            It.Is<DirectMessageUpdatedEvent>(e =>
                e.Message.Id == message.Id &&
                e.Message.Content.SequenceEqual(request.Content)),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddReactionAsync_WithConcurrentReactions_HandlesRaceCondition()
    {
        // Arrange
        await using var context = await _contextFactory.CreateDbContextAsync();
        var message = await CreateMessageWithAttachment(context);
        var tasks = new List<Task<Result<MessageReactionDto>>>();

        // Act
        for (int i = 0; i < 5; i++)
        {
            tasks.Add(_sut.AddChannelMessageReactionAsync(
                messageId: message.Id,
                userId: TestData.Users.User1.Id,
                type: ReactionType.Like,
                ct: CancellationToken.None));
        }

        var results = await Task.WhenAll(tasks);

        // Assert
        Assert.Single(results, r => r.IsSuccess);
        Assert.Equal(4, results.Count(r => r.IsFailure));
    }

    [Fact]
    public async Task UpdateAsync_WithInvalidContent_ReturnsFailure()
    {
        // Arrange
        await using var context = await _contextFactory.CreateDbContextAsync();
        var message = await CreateMessageWithAttachment(context);
        var request = new UpdateMessageRequest(
            Content: new byte[TestData.Settings.LimitSettings.Value.MaxMessageLength + 1],
            ContentType: ContentType.Text,
            MessageNonce: [4, 5, 6],
            KeyVersion: 1);

        // Act
        var result = await _sut.UpdateAsync(message.Id, message.SenderId, request, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal("Validation", result.Error.Code);
    }

    [Fact]
    public async Task UpdateAsync_PublishesEventWithCorrectData()
    {
        // Arrange
        await using var context = await _contextFactory.CreateDbContextAsync();
        var message = await CreateMessageWithAttachment(context);
        var request = new UpdateMessageRequest(
            Content: [1, 2, 3],
            ContentType: ContentType.Text,
            MessageNonce: [4, 5, 6],
            KeyVersion: 2);

        // Act
        var result = await _sut.UpdateAsync(message.Id, message.SenderId, request, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _eventBus.Verify(x => x.PublishAsync(
            It.Is<MessageUpdatedEvent>(e =>
                e.ChannelId == message.ChannelId &&
                e.Message.Id == message.Id &&
                e.Message.KeyVersion == request.KeyVersion),
            It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task GetChannelMessageReactionsAsync_ReturnsOrderedReaction()
    {
        // Arrange
        await using var context = await _contextFactory.CreateDbContextAsync();
        var message = await CreateMessageWithAttachment(context);

        // Add reactions with specific timestamps for ordering verification
        for (int i = 0; i < 3; i++)
        {
            var reaction = new MessageReaction
            {
                ChannelMessageId = message.Id,
                UserId = TestData.Users.User1.Id,
                Type = ReactionType.Custom,
                CustomEmoji = $"emoji_{i}",
                CreatedAt = DateTime.UtcNow.AddMinutes(-i) // Each reaction created 1 minute apart
            };
            context.MessageReactions.Add(reaction);
        }
        await context.SaveChangesAsync();

        // Act
        var result = await _sut.GetChannelMessageReactionsAsync(message.Id, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(3, result.Value.Count);

        // Verify ordering by CreatedAt
        var reactions = result.Value.ToList();
        for (int i = 0; i < reactions.Count - 1; i++)
        {
            Assert.True(reactions[i].CreatedAt <= reactions[i + 1].CreatedAt);
        }
    }

    [Fact]
    public async Task RateLimit_ResetsAfterWindowExpires()
    {
        // Arrange
        var userId = TestData.Users.User1.Id;
        var request = new CreateMessageRequest(
            ChannelId: TestData.Channels.TextChannel.Id,
            Content: [1, 2, 3],
            ContentType: ContentType.Text,
            MessageNonce: [4, 5, 6],
            KeyVersion: 1);

        // Send messages until rate limit is exceeded
        for (int i = 0; i < TestData.Settings.LimitSettings.Value.RateLimits.Messages.Points; i++)
        {
            await _sut.CreateAsync(userId, request);
        }

        var result = await _sut.CreateAsync(userId, request);
        Assert.True(result.IsFailure);
        Assert.Equal("TooManyRequests", result.Error.Code);

        // Wait for rate limit window to expire
        await Task.Delay(TimeSpan.FromSeconds(TestData.Settings.LimitSettings.Value.RateLimits.Messages.DurationSeconds));

        // Should be able to send message again
        result = await _sut.CreateAsync(userId, request);
        Assert.True(result.IsSuccess);
    }

    public void Dispose()
    {
        TestDbContextFactory.Destroy(_contextFactory);
    }
}
