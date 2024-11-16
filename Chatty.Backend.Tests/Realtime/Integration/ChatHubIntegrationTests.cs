using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading;

using Chatty.Backend.Data;
using Chatty.Backend.Data.Models;
using Chatty.Backend.Services.Messages;
using Chatty.Backend.Tests.Helpers;
using Chatty.Shared.Models.Enums;
using Chatty.Shared.Models.Messages;
using Chatty.Shared.Models.Users;

using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using Xunit;

namespace Chatty.Backend.Tests.Realtime.Integration;

public sealed class ChatHubIntegrationTests : IAsyncLifetime, IAsyncDisposable
{
    private readonly TestServer _server;
    private readonly ChattyDbContext _context;
    private HubConnection? _connection1;
    private HubConnection? _connection2;
    private User? _user1;
    private User? _user2;
    private Channel? _channel;

    public ChatHubIntegrationTests()
    {
        _server = new TestServer();
        _context = _server.Services.GetRequiredService<ChattyDbContext>();
    }

    public async Task InitializeAsync()
    {
        // Set up test data
        _user1 = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser1",
            Email = "test1@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(TestData.Auth.DefaultPassword)
        };
        _user2 = new User
        {
            Id = Guid.NewGuid(),
            Username = "testuser2",
            Email = "test2@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(TestData.Auth.DefaultPassword)
        };
        _channel = new Channel
        {
            Id = Guid.NewGuid(),
            Name = "test-channel",
            ChannelType = ChannelType.Text,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _context.Users.AddRangeAsync(_user1, _user2);
        await _context.Channels.AddAsync(_channel);
        await _context.ChannelMembers.AddRangeAsync(
            new ChannelMember { ChannelId = _channel.Id, UserId = _user1.Id },
            new ChannelMember { ChannelId = _channel.Id, UserId = _user2.Id }
        );
        await _context.SaveChangesAsync();

        // Set up SignalR connections
        _connection1 = CreateHubConnection(_user1.Id);
        _connection2 = CreateHubConnection(_user2.Id);

        // Register event handlers for both connections
        RegisterEventHandlers(_connection1);
        RegisterEventHandlers(_connection2);

        // Add event handlers for connection state changes
        _connection1.Closed += ex =>
        {
            if (ex != null)
            {
                Console.WriteLine($"Connection 1 closed with error: {ex.Message}");
            }
            return Task.CompletedTask;
        };

        _connection2.Closed += ex =>
        {
            if (ex != null)
            {
                Console.WriteLine($"Connection 2 closed with error: {ex.Message}");
            }
            return Task.CompletedTask;
        };

        // Wait for connections to be established
        try
        {
            await Task.WhenAll(
                _connection1.StartAsync(CancellationToken.None),
                _connection2.StartAsync(CancellationToken.None)
            );

            // Wait for the connections to be fully established
            var timeout = TimeSpan.FromSeconds(10);
            using var cts = new CancellationTokenSource(timeout);
            while ((_connection1.State != HubConnectionState.Connected ||
                _connection2.State != HubConnectionState.Connected) &&
                !cts.Token.IsCancellationRequested)
            {
                await Task.Delay(100, cts.Token);
            }

            if (_connection1.State != HubConnectionState.Connected ||
                _connection2.State != HubConnectionState.Connected)
            {
                throw new TimeoutException($"Failed to establish connections within {timeout.TotalSeconds} seconds");
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to establish SignalR connections: {ex.Message}", ex);
        }
    }

    private void RegisterEventHandlers(HubConnection connection)
    {
        connection.On<Guid, UserDto>("OnTypingStarted", (channelId, user) =>
        {
            return Task.CompletedTask;
        });

        connection.On<Guid, MessageDto>("OnMessageReceived", (channelId, message) =>
        {
            return Task.CompletedTask;
        });

        connection.On<string, string>("OnNotification", (title, message) =>
        {
            return Task.CompletedTask;
        });

        connection.On<Guid, UserStatus, string>("OnUserPresenceChanged", (userId, status, message) =>
        {
            return Task.CompletedTask;
        });

        connection.On<Guid, bool>("OnUserOnlineStateChanged", (userId, isOnline) =>
        {
            return Task.CompletedTask;
        });
    }

    private HubConnection CreateHubConnection(Guid userId)
    {
        var token = GenerateTestToken(userId);
        return new HubConnectionBuilder()
            .WithUrl(_server.CreateUri("/hubs/chat"), options =>
            {
                options.AccessTokenProvider = () => Task.FromResult<string?>(token);
                options.SkipNegotiation = false;
                options.Transports = HttpTransportType.WebSockets;
            })
            .ConfigureLogging(logging =>
            {
                logging.SetMinimumLevel(LogLevel.Debug);
                logging.AddConsole();
            })
            .WithAutomaticReconnect()
            .Build();
    }

    private string GenerateTestToken(Guid userId)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.UTF8.GetBytes("super_secret_key_for_testing_only_do_not_use_in_production_123");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.NameIdentifier, userId.ToString()),
                new Claim(ClaimTypes.Name, userId.ToString()),
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = "chatty",
            Audience = "chatty-client",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [Fact]
    public async Task JoinChannel_BothUsersReceiveMessages()
    {
        // Arrange
        var messageReceived1 = new TaskCompletionSource<MessageDto>();
        var messageReceived2 = new TaskCompletionSource<MessageDto>();

        _connection1?.On<Guid, MessageDto>("OnMessageReceived", (channelId, message) =>
        {
            messageReceived1.SetResult(message);
            return Task.CompletedTask;
        });

        _connection2?.On<Guid, MessageDto>("OnMessageReceived", (channelId, message) =>
        {
            messageReceived2.SetResult(message);
            return Task.CompletedTask;
        });

        // Act
        await _connection1!.InvokeAsync("JoinChannelAsync", _channel!.Id);
        await _connection2!.InvokeAsync("JoinChannelAsync", _channel!.Id);

        var messageService = _server.Services.GetRequiredService<IMessageService>();
        var message = new CreateMessageRequest(
            ChannelId: _channel.Id,
            Content: System.Text.Encoding.UTF8.GetBytes("Test message"),
            ContentType: ContentType.Text,
            MessageNonce: new byte[24],
            KeyVersion: 1,
            Attachments: null
        );

        await messageService.CreateAsync(_user1!.Id, message);

        // Assert
        var receivedMessage1 = await messageReceived1.Task.WaitAsync(TimeSpan.FromSeconds(5));
        var receivedMessage2 = await messageReceived2.Task.WaitAsync(TimeSpan.FromSeconds(5));

        Assert.NotNull(receivedMessage1);
        Assert.NotNull(receivedMessage2);
        Assert.Equal(_user1.Id, receivedMessage1.Sender.Id);
        Assert.Equal(_user1.Id, receivedMessage2.Sender.Id);
    }

    [Fact]
    public async Task StartTyping_OtherUserReceivesTypingIndicator()
    {
        // Arrange
        var typingReceived = new TaskCompletionSource<(Guid channelId, UserDto user)>();

        _connection2?.On<Guid, UserDto>("OnTypingStarted", (channelId, user) =>
        {
            typingReceived.SetResult((channelId, user));
            return Task.CompletedTask;
        });

        // Act
        await _connection1!.InvokeAsync("JoinChannelAsync", _channel!.Id);
        await _connection2!.InvokeAsync("JoinChannelAsync", _channel!.Id);

        // Add a delay to ensure connections are properly joined
        await Task.Delay(100);

        await _connection1!.InvokeAsync("StartTypingAsync", _channel!.Id);

        // Assert
        var result = await typingReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
        Assert.Equal(_channel!.Id, result.channelId);
        Assert.Equal(_user1!.Id, result.user.Id);
        Assert.Equal(_user1.Username, result.user.Username);
    }

    [Fact]
    public async Task UserGoesOffline_OtherUserReceivesOfflineStatus()
    {
        // Arrange
        var offlineReceived = new TaskCompletionSource<(Guid userId, bool isOnline)>();
        var @lock = new Lock();
        var handled = false;

        _connection2?.On<Guid, bool>("OnUserOnlineStateChanged", (userId, isOnline) =>
        {
            lock (@lock)
            {
                if (!handled && !isOnline)
                {
                    handled = true;
                    offlineReceived.SetResult((userId, isOnline));
                }
            }
            return Task.CompletedTask;
        });

        // Ensure connections are fully established
        await Task.Delay(500);

        // Act
        try
        {
            await _connection1!.StopAsync();

            // Assert
            var result = await offlineReceived.Task.WaitAsync(TimeSpan.FromSeconds(5));
            Assert.Equal(_user1!.Id, result.userId);
            Assert.False(result.isOnline);
        }
        catch (Exception)
        {
            // Ensure connections are properly cleaned up even if test fails
            if (_connection1?.State == HubConnectionState.Connected)
                await _connection1.StopAsync();
            if (_connection2?.State == HubConnectionState.Connected)
                await _connection2.StopAsync();
            throw;
        }
    }

    private async Task Dispose()
    {
        if (_connection1 != null)
        {
            await _connection1.DisposeAsync();
            _connection1 = null;
        }
        if (_connection2 != null)
        {
            await _connection2.DisposeAsync();
            _connection2 = null;
        }
        if (_context != null)
        {
            await _context.Database.EnsureDeletedAsync();
            await _context.DisposeAsync();
        }
        await _server.DisposeAsync();
    }

    async ValueTask IAsyncDisposable.DisposeAsync()
    {
        await Dispose();
    }

    public async Task DisposeAsync()
    {
        await Dispose();
    }
}
