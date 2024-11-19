using Chatty.Client.Cache;
using Chatty.Client.Logging;
using Chatty.Client.Storage;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using Polly;
using Polly.Extensions.Http;

namespace Chatty.Client;

/// <summary>
///     Builder for configuring ChattyClient with platform-specific implementations
/// </summary>
public class ChattyClientBuilder(string baseUrl)
{
    private readonly IServiceCollection _services = new ServiceCollection();
    private TimeSpan _circuitBreakerDuration = TimeSpan.FromSeconds(30);
    private int _exceptionsAllowedBeforeBreaking = 5;

    private int _maxRetries = 3;

    private TimeSpan[] _retryDelays =
    [
        TimeSpan.FromSeconds(1),
        TimeSpan.FromSeconds(2),
        TimeSpan.FromSeconds(5)
    ];

    /// <summary>
    ///     Configures the device manager implementation
    /// </summary>
    public ChattyClientBuilder UseDeviceManager<T>() where T : class, IDeviceManager
    {
        _services.AddSingleton<IDeviceManager, T>();
        return this;
    }

    /// <summary>
    ///     Configures the logger implementation
    /// </summary>
    public ChattyClientBuilder UseLogger<T>() where T : class
    {
        _services.AddLogging(builder => builder.AddProvider(new ServiceCollectionLoggingProvider<T>(_services)));
        return this;
    }

    /// <summary>
    ///     Configures the cache service implementation
    /// </summary>
    public ChattyClientBuilder UseCacheService<T>() where T : class, ICacheService
    {
        _services.AddSingleton<ICacheService, T>();
        return this;
    }

    /// <summary>
    ///     Configures retry delays for HTTP requests
    /// </summary>
    public ChattyClientBuilder ConfigureRetryDelays(params TimeSpan[] delays)
    {
        _retryDelays = delays;
        _maxRetries = delays.Length;
        return this;
    }

    /// <summary>
    ///     Configures maximum number of retries for HTTP requests
    /// </summary>
    public ChattyClientBuilder ConfigureMaxRetries(int maxRetries)
    {
        _maxRetries = maxRetries;
        return this;
    }

    /// <summary>
    ///     Configures circuit breaker settings for HTTP requests
    /// </summary>
    public ChattyClientBuilder ConfigureCircuitBreaker(
        TimeSpan duration,
        int exceptionsAllowedBeforeBreaking)
    {
        _circuitBreakerDuration = duration;
        _exceptionsAllowedBeforeBreaking = exceptionsAllowedBeforeBreaking;
        return this;
    }

    /// <summary>
    ///     Configures additional services
    /// </summary>
    public ChattyClientBuilder ConfigureServices(Action<IServiceCollection> configure)
    {
        configure(_services);
        return this;
    }

    /// <summary>
    ///     Builds the ChattyClient instance
    /// </summary>
    public ChattyClient Build()
    {
        // Ensure required services are registered
        if (!_services.Any(d => d.ServiceType == typeof(IDeviceManager)))
        {
            throw new InvalidOperationException(
                "No device manager implementation registered. Call UseDeviceManager<T>() first.");
        }

        if (!_services.Any(d => d.ServiceType == typeof(ILogger<>)))
        {
            _services.AddLogging(builder =>
            {
                builder.SetMinimumLevel(LogLevel.Debug);
                builder.AddConsole();
            });
        }

        if (!_services.Any(d => d.ServiceType == typeof(ICacheService)))
        {
            _services.AddSingleton<ICacheService, MemoryCacheService>();
        }

        // Configure retry policies
        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(_maxRetries, retryAttempt =>
            {
                var index = Math.Min(retryAttempt - 1, _retryDelays.Length - 1);
                return _retryDelays[index];
            });

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(
                _exceptionsAllowedBeforeBreaking,
                _circuitBreakerDuration);

        // Configure HTTP client
        _services.AddHttpClient("ChattyAPI", client => { client.BaseAddress = new Uri(baseUrl); })
            .AddPolicyHandler(retryPolicy)
            .AddPolicyHandler(circuitBreakerPolicy);

        // Create client
        return new ChattyClient(baseUrl, _services.BuildServiceProvider());
    }
}
