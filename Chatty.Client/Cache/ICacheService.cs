namespace Chatty.Client.Cache;

/// <summary>
///     Service for caching data
/// </summary>
public interface ICacheService
{
    /// <summary>
    ///     Gets a value from the cache
    /// </summary>
    Task<T?> GetAsync<T>(string key, CancellationToken ct = default) where T : class;

    /// <summary>
    ///     Sets a value in the cache
    /// </summary>
    Task SetAsync<T>(string key, T value, TimeSpan? expiry = null, CancellationToken ct = default) where T : class;

    /// <summary>
    ///     Removes a value from the cache
    /// </summary>
    Task RemoveAsync(string key, CancellationToken ct = default);

    /// <summary>
    ///     Gets multiple values from the cache
    /// </summary>
    Task<IReadOnlyDictionary<string, T>> GetManyAsync<T>(IEnumerable<string> keys, CancellationToken ct = default)
        where T : class;

    /// <summary>
    ///     Sets multiple values in the cache
    /// </summary>
    Task SetManyAsync<T>(IDictionary<string, T> values, TimeSpan? expiry = null, CancellationToken ct = default)
        where T : class;

    /// <summary>
    ///     Removes multiple values from the cache
    /// </summary>
    Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken ct = default);

    /// <summary>
    ///     Gets a value from the cache, or sets it if it doesn't exist
    /// </summary>
    Task<T> GetOrSetAsync<T>(
        string key,
        Func<CancellationToken, Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken ct = default) where T : class;

    /// <summary>
    ///     Gets a value from the cache, or sets it if it doesn't exist or is expired
    /// </summary>
    Task<T> GetOrUpdateAsync<T>(
        string key,
        Func<T?, CancellationToken, Task<T>> factory,
        TimeSpan? expiry = null,
        CancellationToken ct = default) where T : class;

    /// <summary>
    ///     Clears all values from the cache
    /// </summary>
    Task ClearAsync(CancellationToken ct = default);
}
