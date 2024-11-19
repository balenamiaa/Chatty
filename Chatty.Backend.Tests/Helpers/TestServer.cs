using System.Text;

using Chatty.Backend.Data;
using Chatty.Backend.Infrastructure.Configuration;
using Chatty.Backend.Realtime;
using Chatty.Backend.Realtime.Events;
using Chatty.Backend.Realtime.Hubs;
using Chatty.Backend.Services.Auth;
using Chatty.Backend.Services.Channels;
using Chatty.Backend.Services.Files;
using Chatty.Backend.Services.Messages;
using Chatty.Backend.Services.Presence;
using Chatty.Backend.Services.Servers;
using Chatty.Backend.Services.Voice;
using Chatty.Shared.Models.Notifications;

using FluentValidation;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using Moq;

using ICryptoProvider = Chatty.Shared.Crypto.ICryptoProvider;

namespace Chatty.Backend.Tests.Helpers;

public sealed class TestServer : IAsyncDisposable
{
    private readonly WebApplication _app;
    private readonly string _databaseName;

    public TestServer()
    {
        _databaseName = $"ChattyTest_{Guid.NewGuid()}";

        var builder = WebApplication.CreateBuilder();

        // Add services
        _ = builder.Services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.HandshakeTimeout = TimeSpan.FromSeconds(2);
            options.KeepAliveInterval = TimeSpan.FromSeconds(1);
            options.ClientTimeoutInterval = TimeSpan.FromSeconds(3);
            options.MaximumParallelInvocationsPerClient = 2;
        }).AddHubOptions<ChatHub>(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
            options.StreamBufferCapacity = 10;
        });
        _ = builder.Services.AddLogging(config =>
        {
            config.AddConsole();
            config.SetMinimumLevel(LogLevel.Debug);
        });
        _ = builder.Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = "Test";
                options.DefaultChallengeScheme = "Test";
            })
            .AddJwtBearer("Test", options =>
            {
                options.RequireHttpsMetadata = false;
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes("super_secret_key_for_testing_only_do_not_use_in_production_123")),
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidIssuer = "chatty",
                    ValidAudience = "chatty-client",
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        if (string.IsNullOrEmpty(accessToken))
                        {
                            var authorization = context.Request.Headers["Authorization"].FirstOrDefault();
                            if (!string.IsNullOrEmpty(authorization) &&
                                authorization.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                            {
                                accessToken = authorization.Substring("Bearer ".Length).Trim();
                            }
                        }

                        var path = context.HttpContext.Request.Path;
                        if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/chat"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    },
                    OnAuthenticationFailed = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TestServer>>();
                        logger.LogError(context.Exception, "Authentication failed");
                        return Task.CompletedTask;
                    },
                    OnTokenValidated = context =>
                    {
                        var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<TestServer>>();
                        logger.LogInformation("Token validated successfully");
                        return Task.CompletedTask;
                    }
                };
            });
        _ = builder.Services.AddAuthorization();

        // Add configuration
        var configuration = new Dictionary<string, string>
        {
            { "Jwt:Key", "super_secret_key_for_testing_only_do_not_use_in_production_123" },
            { "Jwt:Issuer", "chatty" },
            { "Jwt:Audience", "chatty-client" }
        };

        _ = builder.Configuration.AddInMemoryCollection(configuration!);

        // Add database
        _ = builder.Services.AddDbContext<ChattyDbContext>(options =>
            options.UseInMemoryDatabase(_databaseName));

        _ = builder.Services.AddPooledDbContextFactory<ChattyDbContext>(options =>
            options.UseInMemoryDatabase(_databaseName));

        // Add required services
        _ = builder.Services.AddScoped<IFileService, FileService>();
        _ = builder.Services.AddScoped<IVoiceService, VoiceService>();
        _ = builder.Services.AddScoped<IMessageService, MessageService>();
        _ = builder.Services.AddScoped<IChannelService, ChannelService>();
        _ = builder.Services.AddScoped<IServerService, ServerService>();
        _ = builder.Services.AddScoped<IAuthService, AuthService>();
        _ = builder.Services.AddSingleton<IConnectionTracker, ConnectionTracker>();
        _ = builder.Services.AddSingleton<ITypingTracker, TypingTracker>();
        _ = builder.Services.AddSingleton<IPresenceService, PresenceService>();
        _ = builder.Services.AddSingleton<IEventBus, EventBus>();
        _ = builder.Services.AddSingleton<IEventDispatcher, EventDispatcher>();

        // Add mock services
        var mockCrypto = new Mock<ICryptoProvider>();
        mockCrypto.Setup(x => x.GenerateKey())
            .Returns([1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16])
            .Verifiable();
        builder.Services.AddSingleton(mockCrypto.Object);
        builder.Services.AddSingleton(
            new Mock<IValidator<NotificationPreferences>>().Object);

        // Add limit settings
        _ = builder.Services.Configure<LimitSettings>(_ => new LimitSettings
        {
            MaxServersPerUser = 100,
            MaxChannelsPerServer = 500,
            MaxMembersPerServer = 1000,
            RateLimits = new RateLimitSettings
            {
                Messages = new RateLimit
                {
                    Points = 10,
                    DurationSeconds = 60
                },
                Uploads = new RateLimit
                {
                    Points = 10,
                    DurationSeconds = 60
                }
            }
        });

        // Build and configure the app
        _app = builder.Build();

        // Configure middleware
        _ = _app.UseRouting();
        _ = _app.UseAuthentication();
        _ = _app.UseAuthorization();

        // Map endpoints
        _ = _app.MapHub<ChatHub>("/hubs/chat");
        _ = _app.MapGet("/health", () => Results.Ok());

        // Configure the app to use a random port
        var random = new Random();
        var port = random.Next(10000, 65535);
        _app.Urls.Clear();
        _app.Urls.Add($"http://localhost:{port}");

        // Start the server
        _app.StartAsync().GetAwaiter().GetResult();

        // Wait for the server to be ready
        var httpClient = new HttpClient();
        var maxAttempts = 5;
        var attempt = 0;
        var delay = TimeSpan.FromMilliseconds(100);

        while (attempt < maxAttempts)
        {
            try
            {
                var response = httpClient.GetAsync($"http://localhost:{port}/health").GetAwaiter().GetResult();
                if (response.IsSuccessStatusCode)
                {
                    break;
                }
            }
            catch
            {
                // Ignore any exceptions and retry
            }

            Thread.Sleep(delay);
            attempt++;
        }
    }

    public IServiceProvider Services => _app.Services;

    public async ValueTask DisposeAsync()
    {
        await _app.DisposeAsync();

        // Clean up the in-memory database
        var options = new DbContextOptionsBuilder<ChattyDbContext>()
            .UseInMemoryDatabase(_databaseName)
            .Options;

        await using var context = new ChattyDbContext(options);
        _ = await context.Database.EnsureDeletedAsync();
    }

    public Uri CreateUri(string relativePath)
    {
        var address = _app.Urls.First();
        return new Uri($"{address}{relativePath}");
    }
}
