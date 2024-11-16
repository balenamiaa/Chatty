using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Infrastructure.Configuration;
using Chatty.Backend.Realtime.Events;
using Chatty.Backend.Services.Servers;
using Chatty.Backend.Tests.Helpers;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Servers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace Chatty.Backend.Tests.Services.Servers;

public sealed class ServerServiceTests : IDisposable
{
    private readonly ChattyDbContext _context;
    private readonly ServerService _sut;

    public ServerServiceTests()
    {
        _context = TestDbContextFactory.Create();
        Mock<IEventBus> eventBus = new();
        Mock<ILogger<ServerService>> logger = new();

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

        _sut = new ServerService(_context, eventBus.Object, logger.Object, limitSettings);

        SetupTestData();
    }

    [Fact]
    public async Task CreateAsync_WithValidRequest_CreatesServerWithDefaultChannels()
    {
        // Arrange
        var userId = TestData.User1.Id;
        var request = new CreateServerRequest(
            Name: "Test Server",
            IconUrl: null);

        // Act
        var result = await _sut.CreateAsync(userId, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(request.Name, result.Value.Name);

        var channels = await _context.Channels
            .Where(c => c.ServerId == result.Value.Id)
            .ToListAsync();
        Assert.Contains(channels, c => c.Name == "general");
        Assert.Contains(channels, c => c.Name == "voice");
    }

    [Fact]
    public async Task AddMemberAsync_WithInvite_AddsToDefaultChannels()
    {
        // Arrange
        var server = await CreateTestServer();
        var userId = TestData.User2.Id;

        // Act
        var result = await _sut.AddMemberAsync(server.Id, userId);

        // Assert
        Assert.True(result.IsSuccess);
        var memberChannels = await _context.ChannelMembers
            .Where(m => m.Channel.ServerId == server.Id && m.UserId == userId)
            .ToListAsync();
        Assert.NotEmpty(memberChannels);
    }

    [Fact]
    public async Task CreateRoleAsync_WithPermissions_SetsCorrectPermissions()
    {
        // Arrange
        var server = await CreateTestServer();
        var request = new CreateServerRoleRequest(
            Name: "Moderator",
            Color: "#FF0000",
            Position: 1,
            Permissions: new[] { PermissionType.ManageMessages, PermissionType.ManageChannels });

        // Act
        var result = await _sut.CreateRoleAsync(server.Id, request);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(request.Name, result.Value.Name);
        Assert.True(request.Permissions.SequenceEqual(result.Value.Permissions));
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
            Permissions = new List<ServerRolePermission>
            {
                new() { Permission = PermissionType.ViewChannels },
                new() { Permission = PermissionType.SendMessages },
                new() { Permission = PermissionType.ReadMessageHistory },
                new() { Permission = PermissionType.Connect },
                new() { Permission = PermissionType.Speak }
            }
        };

        var ownerMember = new ServerMember
        {
            ServerId = server.Id,
            UserId = TestData.User1.Id,
            RoleId = defaultRole.Id
        };

        // Create default channels
        var generalChannel = new Channel
        {
            Id = Guid.NewGuid(),
            ServerId = server.Id,
            Name = "general",
            ChannelType = ChannelType.Text,
            Position = 0
        };

        var voiceChannel = new Channel
        {
            Id = Guid.NewGuid(),
            ServerId = server.Id,
            Name = "voice",
            ChannelType = ChannelType.Voice,
            Position = 1
        };

        _context.Servers.Add(server);
        _context.ServerRoles.Add(defaultRole);
        _context.ServerMembers.Add(ownerMember);
        _context.Channels.Add(generalChannel);
        _context.Channels.Add(voiceChannel);

        // Add owner to default channels
        var generalMember = new ChannelMember
        {
            ChannelId = generalChannel.Id,
            UserId = TestData.User1.Id
        };

        var voiceMember = new ChannelMember
        {
            ChannelId = voiceChannel.Id,
            UserId = TestData.User1.Id
        };

        _context.ChannelMembers.Add(generalMember);
        _context.ChannelMembers.Add(voiceMember);

        await _context.SaveChangesAsync();
        return server;
    }

    private void SetupTestData()
    {
        _context.Users.AddRange(TestData.User1, TestData.User2);
        _context.SaveChanges();
    }

    public void Dispose()
    {
        TestDbContextFactory.Destroy(_context);
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