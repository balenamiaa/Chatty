using System.Net;
using System.Net.Http.Json;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using Chatty.Client.Cache;
using Chatty.Client.Exceptions;
using Chatty.Client.Http;
using Chatty.Client.Logging;
using Chatty.Client.Models;
using Chatty.Client.Realtime;
using Chatty.Shared.Models.Enums;

using Microsoft.Extensions.Logging;

namespace Chatty.Client.Services;

/// <summary>
///     Service for managing user presence and status using HTTP endpoints
/// </summary>
public sealed class PresenceService : BaseService, IPresenceService, IDisposable
{
    private readonly ICacheService _cache;
    private readonly ILogger<PresenceService> _logger;
    private readonly Subject<(Guid UserId, bool IsOnline)> _onlineStateChanged = new();
    private readonly IChattyRealtimeClient _realtimeClient;

    private readonly Subject<(Guid UserId, UserStatus Status, string? StatusMessage)> _statusChanged = new();

    public PresenceService(
        IChattyRealtimeClient realtimeClient,
        ICacheService cache,
        ILogger<PresenceService> logger,
        IHttpClientFactory httpClientFactory)
        : base(httpClientFactory, logger, "Presence")
    {
        _realtimeClient = realtimeClient;
        _cache = cache;
        _logger = logger;

        // Subscribe to real-time events
        _realtimeClient.OnUserPresenceChanged.Subscribe(ev =>
        {
            _onlineStateChanged.OnNext((ev.UserId, ev.IsOnline));
        });

        _realtimeClient.OnUserStatusChanged.Subscribe(ev => { _statusChanged.OnNext((ev.UserId, ev.Status, null)); });
    }

    public void Dispose()
    {
        _statusChanged.Dispose();
        _onlineStateChanged.Dispose();
    }

    public IObservable<(Guid UserId, UserStatus Status, string? StatusMessage)> OnStatusChanged =>
        _statusChanged.AsObservable();

    public IObservable<(Guid UserId, bool IsOnline)> OnOnlineStateChanged => _onlineStateChanged.AsObservable();

    public async Task UpdateStatusAsync(
        UserStatus status,
        string? statusMessage = null,
        CancellationToken ct = default)
    {
        try
        {
            await _realtimeClient.UpdateStatusAsync(status, statusMessage);

            _logger.LogInformation(
                "Updated status to {Status} with message {StatusMessage}",
                status,
                statusMessage ?? "null");
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to update status to {Status} with message {StatusMessage}",
                status,
                statusMessage ?? "null");
            throw;
        }
    }

    public async Task<UserStatus> GetUserStatusAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        try
        {
            var cacheKey = CacheKeys.UserStatus(userId);
            var cachedStatus = await _cache.GetAsync<UserStatusState>(cacheKey, ct);
            if (cachedStatus is { Value: var cachedStatusValue })
            {
                _logger.LogCacheHit(cacheKey);
                return cachedStatusValue;
            }

            _logger.LogCacheMiss(cacheKey);
            var endpoint = ApiEndpoints.Presence.UserStatus(userId);
            _logger.LogHttpRequest("GET", endpoint);

            // Get from server via HTTP
            var status = await ExecuteWithPoliciesAsync(async client =>
            {
                var response = await client.GetAsync(endpoint, ct);
                _logger.LogHttpResponse("GET", endpoint, (int)response.StatusCode);
                return await response.Content.ReadFromJsonAsync<UserStatus?>(ct);
            }, ct);

            if (status == null)
            {
                throw new ApiException(
                    $"Failed to get status for user {userId}",
                    HttpStatusCode.InternalServerError);
            }

            // Cache the result
            await _cache.SetAsync(
                cacheKey,
                new UserStatusState(status.Value),
                TimeSpan.FromMinutes(5),
                ct);

            return status.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get status for user {UserId}",
                userId);
            throw;
        }
    }

    public async Task<IReadOnlyDictionary<Guid, UserStatus>> GetUsersStatusAsync(
        IEnumerable<Guid> userIds,
        CancellationToken ct = default)
    {
        try
        {
            var result = new Dictionary<Guid, UserStatus>();
            var idsToFetch = new List<Guid>();

            // Try to get from cache first
            foreach (var userId in userIds)
            {
                var cacheKey = CacheKeys.UserStatus(userId);
                var cachedStatus = await _cache.GetAsync<UserStatusState>(cacheKey, ct);
                if (cachedStatus is { Value: var cachedStatusValue })
                {
                    _logger.LogCacheHit(cacheKey);
                    result[userId] = cachedStatusValue;
                }
                else
                {
                    _logger.LogCacheMiss(cacheKey);
                    idsToFetch.Add(userId);
                }
            }

            if (idsToFetch.Count > 0)
            {
                _logger.LogHttpRequest("POST", ApiEndpoints.Presence.UserStatusBatch);

                // Get remaining statuses via batch endpoint
                var statuses = await ExecuteWithPoliciesAsync(async client =>
                {
                    var response = await client.PostAsJsonAsync(ApiEndpoints.Presence.UserStatusBatch, idsToFetch, ct);
                    _logger.LogHttpResponse("POST", ApiEndpoints.Presence.UserStatusBatch, (int)response.StatusCode);
                    return await response.Content.ReadFromJsonAsync<Dictionary<Guid, UserStatus>?>(ct);
                }, ct) ?? throw new ApiException(
                    "Failed to get batch user statuses",
                    HttpStatusCode.InternalServerError);

                // Cache results

                foreach (var (userId, status) in statuses)
                {
                    var cacheKey = CacheKeys.UserStatus(userId);
                    await _cache.SetAsync(
                        cacheKey,
                        new UserStatusState(status),
                        TimeSpan.FromMinutes(5),
                        ct);

                    result[userId] = status;
                }
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Failed to get status for users");
            throw;
        }
    }
}
