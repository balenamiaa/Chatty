using Chatty.Backend.Data;
using Chatty.Backend.Services.Auth;
using Chatty.Backend.Services.Presence;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Chatty.Backend.Infrastructure.Configuration;
using Chatty.Backend.Realtime;
using Chatty.Backend.Realtime.Events;
using Chatty.Backend.Security.DeviceVerification;
using Chatty.Backend.Security.KeyBackup;
using Chatty.Backend.Security.KeyRotation;
using Chatty.Backend.Services.Background;
using Chatty.Backend.Services.Channels;
using Chatty.Backend.Services.Contacts;
using Chatty.Backend.Services.Files;
using Chatty.Backend.Services.Messages;
using Chatty.Backend.Services.Servers;
using Chatty.Backend.Services.Stickers;
using Chatty.Backend.Services.Users;
using Chatty.Backend.Services.Voice;
using Chatty.Shared.Crypto;
using Chatty.Shared.Crypto.KeyExchange;
using Chatty.Shared.Crypto.Session;
using Chatty.Shared.Models.Validation;

namespace Chatty.Backend.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Add DbContext
        services.AddDbContext<ChattyDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString("DefaultConnection"),
                o => o.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)
            ));

        // Add Authentication
        services.AddAuthentication()
                .AddJwtBearer();

        services.AddAuthorization();

        // Add Validation
        services.AddValidatorsFromAssemblyContaining<CreateUserRequestValidator>();

        // Add Services
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPresenceService, PresenceService>();
        services.AddScoped<IMessageService, MessageService>();
        services.AddScoped<IStickerService, StickerService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<IServerService, ServerService>();
        services.AddScoped<IChannelService, ChannelService>();
        services.AddScoped<IContactService, ContactService>();
        services.AddScoped<IFileService, FileService>();
        services.AddScoped<IVoiceService, VoiceService>();

        // Add SignalR
        services.AddSignalR(options =>
        {
            options.EnableDetailedErrors = true;
            options.MaximumReceiveMessageSize = 1024 * 1024; // 1MB
            options.StreamBufferCapacity = 10;
        });

        // Add realtime services
        services.AddSingleton<IConnectionTracker, ConnectionTracker>();
        services.AddSingleton<ITypingTracker, TypingTracker>();
        services.AddSingleton<IEventBus, EventBus>();
        services.AddScoped<IEventDispatcher, EventDispatcher>();

        // Add Crypto services
        services.AddSingleton<ICryptoProvider, CryptoProvider>();
        services.AddSingleton<IKeyExchangeService, KeyExchangeService>();
        services.AddSingleton<ISessionManager, SessionManager>();

        // Add background services
        services.AddHostedService<MessageCleanupService>();
        services.AddHostedService<FileCleanupService>();
        services.AddHostedService<PresenceUpdateService>();

        // Add Security services
        services.AddSingleton<IKeyRotationService, KeyRotationService>();
        services.AddSingleton<IKeyBackupService, KeyBackupService>();
        services.AddSingleton<IDeviceVerificationService, DeviceVerificationService>();

        // Add Configuration
        services.Configure<JwtSettings>(configuration.GetSection("Jwt"));
        services.Configure<StorageSettings>(configuration.GetSection("Storage"));
        services.Configure<SecuritySettings>(configuration.GetSection("Security"));
        services.Configure<NotificationSettings>(configuration.GetSection("Notifications"));
        services.Configure<LimitSettings>(configuration.GetSection("Limits"));

        return services;
    }
}