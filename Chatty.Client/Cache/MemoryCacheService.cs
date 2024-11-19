using Microsoft.Extensions.Caching.Memory;

namespace Chatty.Client.Cache;

/// <summary>
///     In-memory implementation of cache service
/// </summary>
public class MemoryCacheService(IMemoryCache cache) : ICacheService
{
    private readonly SemaphoreSlim _lock = new(1, 1);

    public Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class =>
        Task.FromResult(cache.Get<T>(key));

    public Task SetAsync<T>(
        string key,
        T value,
        TimeSpan? expiry = null,
        CancellationToken ct = default) where T : class
    {
        var options = new MemoryCacheEntryOptions();
        if (expiry.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiry;
        }

        cache.Set(key, value, options);
        return Task.CompletedTask;
    }

    public Task RemoveAsync(string key, CancellationToken ct = default)
    {
        cache.Remove(key);
        return Task.CompletedTask;
    }

    public Task<IReadOnlyDictionary<string, T>> GetManyAsync<T>(
        IEnumerable<string> keys,
        CancellationToken ct = default) where T : class
    {
        var result = new Dictionary<string, T>();
        foreach (var key in keys)
        {
            var value = cache.Get<T>(key);
            if (value is not null)
            {
                result[key] = value;
            }
        }

        return Task.FromResult<IReadOnlyDictionary<string, T>>(result);
    }

    public Task SetManyAsync<T>(
        IDictionary<string, T> values,
        TimeSpan? expiry = null,
        CancellationToken ct = default) where T : class
    {
        var options = new MemoryCacheEntryOptions();
        if (expiry.HasValue)
        {
            options.AbsoluteExpirationRelativeToNow = expiry;
        }

        foreach (var (key, value) in values)
        {
            cache.Set(key, value, options);
        }

        return Task.CompletedTask;
    }

    public Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken ct = default)
    {
        foreach (var key in keys)
        {
            cache.Remove(key);
        }

        return Task.CompletedTask;
    }

    public async Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken ct = default) where T : class
    {
        var value = await GetAsync<T>(key, ct);
        if (value is not null)
        {
            return value;
        }

        await _lock.WaitAsync(ct);
        try
        {
            // Check again in case another thread set the value
            value = await GetAsync<T>(key, ct);
            if (value is not null)
            {
                return value;
            }

            value = await factory(ct);
            await SetAsync(key, value, expiry, ct);
            return value;
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<T> GetOrUpdateAsync<T>(
        string key,
        Func<T?, CancellationToken, Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken ct = default) where T : class
    {
        await _lock.WaitAsync(ct);
        try
        {
            // Get current value while holding the lock
            var currentValue = await GetAsync<T>(key, ct);

            // Update the value
            var newValue = await factory(currentValue, ct);
            await SetAsync(key, newValue, expiry, ct);
            return newValue;
        }
        finally
        {
            _lock.Release();
        }
    }

    public Task ClearAsync(CancellationToken ct = default)
    {
        if (cache is MemoryCache memoryCache)
        {
            memoryCache.Compact(1.0);
        }

        return Task.CompletedTask;
    }
}
