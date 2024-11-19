using Chatty.Client.Cache;
using Chatty.Client.Connection;
using Chatty.Client.Logging;
using Chatty.Client.Realtime;
using Chatty.Client.Services;
using Chatty.Client.Services.Messages;
using Chatty.Client.State;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.Extensions.Http;

namespace Chatty.Client;

/// <summary>
///     Main client for interacting with the Chatty backend
/// </summary>
public class ChattyClient
{
    private readonly IConnectionManager _connectionManager;

    internal ChattyClient(string baseUrl, IServiceProvider? services = null)
    {
        // Setup services
        var serviceCollection = new ServiceCollection();
        ConfigureServices(serviceCollection, baseUrl);
        var services1 = services ?? serviceCollection.BuildServiceProvider();

        services1.GetRequiredService<HttpClient>();
        services1.GetRequiredService<IChattyRealtimeClient>();
        _connectionManager = services1.GetRequiredService<IConnectionManager>();
        services1.GetRequiredService<IStateManager>();

        // Initialize service properties
        Messages = services1.GetRequiredService<IMessageService>();
    }

    #region Services

    /// <summary>
    ///     Service for managing messages and direct messages
    /// </summary>
    public IMessageService Messages { get; }

    #endregion

    private void ConfigureServices(IServiceCollection services, string baseUrl)
    {
        // Configure retry policies
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(3, retryAttempt =>
                TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)));

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

        // Configure HTTP client
        services.AddHttpClient("ChattyAPI", client => { client.BaseAddress = new Uri(baseUrl); })
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(circuitBreakerPolicy);

        services.AddScoped(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient("ChattyAPI"));

        // Add core services
        services.AddSingleton<ILogger, ConsoleLogger>();
        services.AddSingleton<ICacheService, MemoryCacheService>();
        services.AddSingleton<IStateManager, MemoryStateManager>();
        services.AddSingleton<IConnectionManager, ConnectionManager>();
        services.AddSingleton<IRealtimeSyncService, RealtimeSyncService>();

        // Configure ChattyRealtimeClient
        services.AddSingleton<IChattyRealtimeClient>(sp => new ChattyRealtimeClient(
            sp,
            sp.GetRequiredService<ILogger<ChattyRealtimeClient>>(),
            sp.GetRequiredService<IConnectionManager>(),
            sp.GetRequiredService<IStateManager>(),
            sp.GetRequiredService<IAuthService>(),
            baseUrl
        ));

        // Add services
        services.AddScoped<IMessageService, MessageService>();
    }

    /// <summary>
    ///     Creates a new builder for configuring the ChattyClient
    /// </summary>
    public static ChattyClientBuilder CreateBuilder(string baseUrl) => new(baseUrl);

    #region Connection Management

    /// <summary>
    ///     Connects to the realtime server and starts listening for events
    /// </summary>
    public Task ConnectAsync(string token, CancellationToken ct = default) =>
        _connectionManager.ConnectAsync(token, ct);

    /// <summary>
    ///     Disconnects from the realtime server
    /// </summary>
    public Task DisconnectAsync(CancellationToken ct = default) => _connectionManager.DisconnectAsync(ct);

    #endregion
}
