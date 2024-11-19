using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Infrastructure.Configuration;
using Chatty.Backend.Realtime.Events;
using Chatty.Backend.Services.Channels;
using Chatty.Backend.Tests.Helpers;
using Chatty.Shared.Models.Channels;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Realtime.Events;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using Moq;

using Xunit;

namespace Chatty.Backend.Tests.Services.Channels;

public sealed class ChannelServiceTests : IDisposable
{
    private readonly ChattyDbContext _context;
    private readonly Mock<IEventBus> _eventBus;
    private readonly ChannelService _sut;

    public ChannelServiceTests()
    {
        _context = TestDbContextFactory.Create();
        _eventBus = new Mock<IEventBus>();
        Mock<ILogger<ChannelService>> logger = new();

        var limitSettings = Options.Create(new LimitSettings
        {
            MaxServersPerUser = 100,
            MaxChannelsPerServer = 100,
            MaxMembersPerServer = 100,
            RateLimits = new RateLimitSettings
            {
                Messages = new RateLimit
                {
                    DurationSeconds = 60,
                    Points = 10
                },
                Uploads = new RateLimit
                {
                    DurationSeconds = 60,
                    Points = 10
                }
            }
        });

        _sut = new ChannelService(_context, _eventBus.Object, logger.Object, limitSettings);

        SetupTestData();
    }

    public void Dispose() => TestDbContextFactory.Destroy(_context);

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesChannel()
    {
        // Arrange
        var server = await CreateTestServer();
        var request = new CreateChannelRequest(
            "test-channel",
            "Test Channel",
            false,
            ChannelType.Text,
            0,
            0);

        // Act
        var result = await _sut.CreateAsync(server.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(request.Name, result.Value.Name);
        Assert.Equal(request.ChannelType, result.Value.ChannelType);

        _eventBus.Verify(x => x.PublishAsync(
                It.Is<ChannelCreatedEvent>(e =>
                    e.ServerId == server.Id &&
                    e.Channel.Id == result.Value.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task AddMemberAsync_WithValidPermissions_AddsMember()
    {
        // Arrange
        var channel = await CreateTestChannel();
        var userId = TestData.User2.Id;

        // Act
        var result = await _sut.AddMemberAsync(channel.Id, userId);

        // Assert
        Assert.True(result.IsSuccess);
        var membership = await _context.ChannelMembers
            .FirstOrDefaultAsync(m => m.ChannelId == channel.Id && m.UserId == userId);
        Assert.NotNull(membership);
    }

    [Fact]
    public async Task UpdateAsync_WithValidRequest_UpdatesChannel()
    {
        // Arrange
        var channel = await CreateTestChannel();
        var request = new UpdateChannelRequest(
            "updated-channel",
            "Updated Channel",
            1,
            5);

        // Act
        var result = await _sut.UpdateAsync(channel.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(request.Name, result.Value.Name);
        Assert.Equal(request.Topic, result.Value.Topic);
    }

    [Fact]
    public async Task GetForServerAsync_ReturnsCorrectChannels()
    {
        // Arrange
        var server = await CreateTestServer();
        var channels = new List<Channel>();
        for (var i = 0; i < 3; i++)
        {
            channels.Add(new Channel
            {
                Id = Guid.NewGuid(),
                ServerId = server.Id,
                Name = $"channel-{i}",
                ChannelType = ChannelType.Text
            });
        }

        _context.Channels.AddRange(channels);
        await _context.SaveChangesAsync();

        // Act
        var result = await _sut.GetForServerAsync(server.Id);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(channels.Count, result.Value.Count);
    }

    private async Task<Server> CreateTestServer()
    {
        var server = new Server
        {
            Id = Guid.NewGuid(),
            Name = "Test Server",
            OwnerId = TestData.User1.Id
        };

        var defaultRole = new ServerRole
        {
            ServerId = server.Id,
            Name = "@everyone",
            IsDefault = true,
            Position = 0,
            Permissions =
            [
                new ServerRolePermission { Permission = PermissionType.ViewChannels },
                new ServerRolePermission { Permission = PermissionType.SendMessages },
                new ServerRolePermission { Permission = PermissionType.ReadMessageHistory },
                new ServerRolePermission { Permission = PermissionType.Connect },
                new ServerRolePermission { Permission = PermissionType.Speak }
            ]
        };

        var ownerMember = new ServerMember
        {
            ServerId = server.Id,
            UserId = TestData.User1.Id,
            RoleId = defaultRole.Id
        };

        _context.Servers.Add(server);
        _context.ServerRoles.Add(defaultRole);
        _context.ServerMembers.Add(ownerMember);
        await _context.SaveChangesAsync();
        return server;
    }

    private async Task<Channel> CreateTestChannel()
    {
        var server = await CreateTestServer();
        var channel = new Channel
        {
            Id = Guid.NewGuid(),
            ServerId = server.Id,
            Name = "test-channel",
            ChannelType = ChannelType.Text
        };
        _context.Channels.Add(channel);
        await _context.SaveChangesAsync();
        return channel;
    }

    private void SetupTestData()
    {
        _context.Users.AddRange(TestData.User1, TestData.User2);
        _context.SaveChanges();
    }

    private static class TestData
    {
        public static readonly User User1 = new()
        {
            Id = Guid.NewGuid(),
            Username = "testuser1",
            Email = "test1@example.com",
            PasswordHash = "hash"
        };

        public static readonly User User2 = new()
        {
            Id = Guid.NewGuid(),
            Username = "testuser2",
            Email = "test2@example.com",
            PasswordHash = "hash"
        };
    }
}
