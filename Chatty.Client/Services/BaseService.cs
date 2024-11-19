using Microsoft.Extensions.Logging;

using Polly;
using Polly.CircuitBreaker;
using Polly.Retry;

namespace Chatty.Client.Services;

/// <summary>
///     Base class for services with HTTP client factory and circuit breaker support
/// </summary>
public abstract class BaseService
{
    private readonly AsyncCircuitBreakerPolicy _circuitBreaker;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger _logger;
    private readonly AsyncRetryPolicy _retryPolicy;
    private readonly string _serviceName;

    protected BaseService(
        IHttpClientFactory httpClientFactory,
        ILogger logger,
        string serviceName)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _serviceName = serviceName;

        // Configure retry policy
        _retryPolicy = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .WaitAndRetryAsync(
                3,
                retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt)),
                (exception, timeSpan, retryCount, context) =>
                {
                    _logger.LogWarning(exception,
                        "Retrying request for service {Service}, attempt {Attempt}, next retry in {NextRetry}",
                        _serviceName, retryCount, timeSpan);
                });

        // Configure circuit breaker
        _circuitBreaker = Policy
            .Handle<HttpRequestException>()
            .Or<TaskCanceledException>()
            .Or<TimeoutException>()
            .CircuitBreakerAsync(
                5,
                TimeSpan.FromSeconds(30),
                (exception, duration) =>
                {
                    _logger.LogWarning(exception, "Circuit breaker tripped for service {Service}, duration: {Duration}",
                        _serviceName, duration);
                },
                () =>
                {
                    _logger.LogInformation("Circuit breaker reset for service {Service}",
                        _serviceName);
                },
                () =>
                {
                    _logger.LogInformation("Circuit breaker half-open for service {Service}",
                        _serviceName);
                });
    }

    protected async Task<T?> ExecuteWithPoliciesAsync<T>(
        Func<HttpClient, Task<T?>> action,
        CancellationToken ct = default) =>
        await _retryPolicy
            .WrapAsync(_circuitBreaker)
            .ExecuteAsync(async () =>
            {
                using var client = _httpClientFactory.CreateClient(_serviceName);
                return await action(client);
            });

    protected async Task<HttpResponseMessage> ExecuteWithPoliciesAsync(
        Func<HttpClient, Task<HttpResponseMessage>> action,
        CancellationToken ct = default) =>
        await _retryPolicy
            .WrapAsync(_circuitBreaker)
            .ExecuteAsync(async () =>
            {
                using var client = _httpClientFactory.CreateClient(_serviceName);
                return await action(client);
            });
}
