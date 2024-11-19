using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;

using Chatty.Client.Logging;

using Microsoft.Extensions.Logging;

namespace Chatty.Client.State;

/// <summary>
///     In-memory implementation of state manager
/// </summary>
public class MemoryStateManager(ILogger<MemoryStateManager> logger) : IStateManager, IDisposable
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly ConcurrentDictionary<string, object> _state = new();
    private readonly ConcurrentDictionary<string, Subject<StateChange<object>>> _subjects = new();
    private readonly ConcurrentDictionary<string, object> _temporaryState = new();

    public void Dispose()
    {
        foreach (var subject in _subjects.Values)
        {
            subject.Dispose();
        }

        _lock.Dispose();
    }

    public async Task<T?> GetStateAsync<T>(string key, CancellationToken ct = default) where T : class
    {
        try
        {
            if (_state.TryGetValue(key, out var value))
            {
                return value as T;
            }

            return null;
        }
        catch (Exception ex)
        {
            logger.Error("Failed to get state", ex,
                ("Key", key),
                ("Type", typeof(T).Name));
            throw;
        }
    }

    public async Task SetStateAsync<T>(string key, T value, CancellationToken ct = default) where T : class
    {
        try
        {
            await _lock.WaitAsync(ct);
            try
            {
                var oldValue = _state.TryGetValue(key, out var existing) ? existing as T : null;
                _state[key] = value;

                NotifyStateChange(key, oldValue, value,
                    oldValue == null ? StateChangeType.Created : StateChangeType.Updated);

                logger.Debug("Set state",
                    ("Key", key),
                    ("Type", typeof(T).Name));
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            logger.Error("Failed to set state", ex,
                ("Key", key),
                ("Type", typeof(T).Name));
            throw;
        }
    }

    public async Task UpdateStateAsync<T>(
        string key,
        Func<T?, T> transform,
        CancellationToken ct = default) where T : class
    {
        try
        {
            await _lock.WaitAsync(ct);
            try
            {
                var oldValue = _state.TryGetValue(key, out var existing) ? existing as T : null;
                var newValue = transform(oldValue);
                _state[key] = newValue;

                NotifyStateChange(key, oldValue, newValue,
                    oldValue == null ? StateChangeType.Created : StateChangeType.Updated);

                logger.Debug("Updated state",
                    ("Key", key),
                    ("Type", typeof(T).Name));
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            logger.Error("Failed to update state", ex,
                ("Key", key),
                ("Type", typeof(T).Name));
            throw;
        }
    }

    public async Task RemoveStateAsync(string key, CancellationToken ct = default)
    {
        try
        {
            await _lock.WaitAsync(ct);
            try
            {
                if (_state.TryRemove(key, out var oldValue))
                {
                    NotifyStateChange(key, oldValue, null, StateChangeType.Deleted);
                }

                logger.Debug("Removed state",
                    ("Key", key));
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            logger.Error("Failed to remove state", ex,
                ("Key", key));
            throw;
        }
    }

    public async Task ClearStateAsync(CancellationToken ct = default)
    {
        try
        {
            await _lock.WaitAsync(ct);
            try
            {
                foreach (var key in _state.Keys.ToList())
                {
                    if (_state.TryRemove(key, out var oldValue))
                    {
                        NotifyStateChange(key, oldValue, null, StateChangeType.Cleared);
                    }
                }

                logger.Debug("Cleared state");
            }
            finally
            {
                _lock.Release();
            }
        }
        catch (Exception ex)
        {
            logger.Error("Failed to clear state", ex);
            throw;
        }
    }

    public async Task SaveTemporaryState()
    {
        try
        {
            await _lock.WaitAsync();
            _temporaryState.Clear();
            foreach (var kvp in _state)
            {
                _temporaryState[kvp.Key] = kvp.Value;
            }

            logger.LogInformation("Saved temporary state with {Count} items", _temporaryState.Count);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task RestoreTemporaryState()
    {
        try
        {
            await _lock.WaitAsync();
            foreach (var kvp in _temporaryState)
            {
                var oldValue = _state.TryGetValue(kvp.Key, out var existing) ? existing : null;
                _state[kvp.Key] = kvp.Value;
                NotifyStateChange(kvp.Key, oldValue, kvp.Value,
                    oldValue == null ? StateChangeType.Created : StateChangeType.Updated);
            }

            logger.LogInformation("Restored temporary state with {Count} items", _temporaryState.Count);
            _temporaryState.Clear();
        }
        finally
        {
            _lock.Release();
        }
    }

    public IObservable<StateChange<T>> ObserveState<T>(string key) where T : class
    {
        var subject = _subjects.GetOrAdd(key, _ => new Subject<StateChange<object>>());
        return subject
            .Where(change => change.OldValue is T || change.NewValue is T)
            .Select(change => new StateChange<T>(
                change.Key,
                change.OldValue as T,
                change.NewValue as T,
                change.ChangeType));
    }

    public Task<IReadOnlyList<string>> GetKeysAsync(string prefix, CancellationToken ct = default)
    {
        try
        {
            var keys = _state.Keys
                .Where(k => k.StartsWith(prefix))
                .OrderBy(k => k)
                .ToList();

            return Task.FromResult<IReadOnlyList<string>>(keys);
        }
        catch (Exception ex)
        {
            logger.Error("Failed to get keys", ex,
                ("Prefix", prefix));
            throw;
        }
    }

    private void NotifyStateChange<T>(string key, T? oldValue, T? newValue, StateChangeType changeType)
        where T : class
    {
        if (_subjects.TryGetValue(key, out var subject))
        {
            subject.OnNext(new StateChange<object>(key, oldValue, newValue, changeType));
        }
    }
}
