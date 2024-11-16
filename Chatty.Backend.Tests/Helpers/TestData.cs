using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Infrastructure.Configuration;
using Chatty.Shared.Models.Enums;

using Microsoft.Extensions.Options;

namespace Chatty.Backend.Tests.Helpers;

public static class TestData
{
    public static class Settings
    {
        public static IOptions<LimitSettings> LimitSettings => Options.Create(new LimitSettings
        {
            MaxMessageLength = 1024,
            RateLimits = new RateLimitSettings
            {
                Messages = new RateLimit
                {
                    Points = 10,
                    DurationSeconds = 5 // Low for testing
                }
            }
        });

    }
    public static class Auth
    {
        public const string DefaultPassword = "password123";
    }

    public static class Users
    {
        public static User User1 => new()
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Username = "testuser1",
            Email = "test1@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Auth.DefaultPassword),
            FirstName = "Test",
            LastName = "User1",
            Locale = "en-US",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        public static User User2 => new()
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            Username = "testuser2",
            Email = "test2@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Auth.DefaultPassword),
            FirstName = "Test",
            LastName = "User2",
            Locale = "en-US",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        public static User User3 => new()
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Username = "testuser3",
            Email = "test3@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(Auth.DefaultPassword),
            FirstName = "Test",
            LastName = "User3",
            Locale = "en-US",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static class Channels
    {
        public static Channel TextChannel => new()
        {
            Id = Guid.Parse("33333333-3333-3333-3333-333333333333"),
            Name = "general",
            ChannelType = ChannelType.Text,
            Position = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        public static Channel VoiceChannel => new()
        {
            Id = Guid.Parse("44444444-4444-4444-4444-444444444444"),
            Name = "voice",
            ChannelType = ChannelType.Voice,
            Position = 1,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        public static Channel PrivateChannel => new()
        {
            Id = Guid.Parse("55555555-5555-5555-5555-555555555555"),
            Name = "private",
            ChannelType = ChannelType.Text,
            IsPrivate = true,
            Position = 2,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static class Servers
    {
        public static Server Server1 => new()
        {
            Id = Guid.Parse("66666666-6666-6666-6666-666666666666"),
            Name = "Test Server",
            OwnerId = Users.User1.Id,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static class Messages
    {
        public static Message TextMessage => new()
        {
            Id = Guid.Parse("77777777-7777-7777-7777-777777777777"),
            ChannelId = Channels.TextChannel.Id,
            SenderId = Users.User1.Id,
            Content = [1, 2, 3],
            ContentType = ContentType.Text,
            MessageNonce = [4, 5, 6],
            KeyVersion = 1,
            SentAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    public static class Devices
    {
        public static UserDevice Device1 => new()
        {
            Id = Guid.Parse("88888888-8888-8888-8888-888888888888"),
            UserId = Users.User1.Id,
            DeviceId = Guid.Parse("99999999-9999-9999-9999-999999999999"),
            DeviceName = "Test Device",
            DeviceType = DeviceType.Web,
            PublicKey = [1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16],
            LastActiveAt = DateTime.UtcNow,
            CreatedAt = DateTime.UtcNow
        };
    }

    public static class TestDbSeeder
    {
        public static void SeedBasicTestData(ChattyDbContext context)
        {
            // Add users
            context.Users.AddRange(Users.User1, Users.User2);

            // Add channels
            context.Channels.AddRange(Channels.TextChannel, Channels.VoiceChannel, Channels.PrivateChannel);

            // Add server
            context.Servers.Add(Servers.Server1);

            // Add channel memberships
            context.ChannelMembers.AddRange(
                new ChannelMember { ChannelId = Channels.TextChannel.Id, UserId = Users.User1.Id },
                new ChannelMember { ChannelId = Channels.TextChannel.Id, UserId = Users.User2.Id },
                new ChannelMember { ChannelId = Channels.VoiceChannel.Id, UserId = Users.User1.Id }
            );

            // Add messages
            context.Messages.Add(Messages.TextMessage);

            // Add devices
            context.UserDevices.Add(Devices.Device1);

            context.SaveChanges();
        }
    }
}
