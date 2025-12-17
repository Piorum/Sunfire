using System.Collections.Concurrent;

namespace Sunfire.Data;

public interface ICache<TSelf, TDomainType, TKey>
    where TSelf : ICache<TSelf, TDomainType, TKey>
    where TKey : notnull
{
    static abstract Task<TDomainType> FetchSingleAsync(TKey id);
    static abstract Task<IEnumerable<TDomainType>> FetchMultipleAsync(IEnumerable<TKey> ids);
    static abstract Task<TKey> GetKey(TDomainType domainType);
}

public abstract class CacheBase<TDerived, TDomainType, TKey>
    where TDerived : ICache<TDerived, TDomainType, TKey>
    where TKey : notnull
{
    protected static readonly ConcurrentDictionary<TKey, ManualResetEventSlim> _processing = [];
    protected static readonly ConcurrentDictionary<TKey, Lazy<Task<TDomainType>>> _cache = [];

    public static Task<TDomainType> GetAsync(TKey id, CancellationToken token = default)
    {
        //If this key is being processed wait for it.
        if (_processing.TryGetValue(id, out var mres))
            mres.Wait(token);

        //GetOrAdd Lazy Task
        var lazyTask = _cache.GetOrAdd(id, _ =>
            new Lazy<Task<TDomainType>>(() => FetchInternalAsync(id))
        );

        return lazyTask.Value;
    }

    public static async Task<IEnumerable<TDomainType>> GetAsync(IEnumerable<TKey> ids, CancellationToken token = default)
    {
        //Get all ids which are not processing or cached
        var distinctIds = ids.Distinct().ToList();
        var missingIds = distinctIds
            .Where(id => !_cache.ContainsKey(id) && !_processing.ContainsKey(id))
            .ToList();
            
        //Ensure we can acquire processing lock for missing Ids
        List<TKey> idsToFetch = [];
        foreach (var id in missingIds)
        {
            ManualResetEventSlim mres = new();
            if (_processing.TryAdd(id, mres))
            {
                mres.Reset();
                idsToFetch.Add(id);
            }
        }

        //Fetch any ids that we need
        if (idsToFetch.Count != 0)
        {
            try 
            {
                var fetchedObjects = await FetchInternalAsync(idsToFetch);

                //Populate Cache and Release Locks
                foreach (var obj in fetchedObjects)
                {
                    var id = await TDerived.GetKey(obj);
                    
                    // Add to cache as a completed task
                    _cache.TryAdd(id, new Lazy<Task<TDomainType>>(() => Task.FromResult(obj)));
                    
                    ReleaseLock(id);
                }
            }
            finally
            {
                //Ensure locks are released even if fetch fails or returns partial results
                foreach(var id in idsToFetch)
                {
                    ReleaseLock(id);
                }
            }
        }

        //Return results from cache
        var tasks = distinctIds.Select((id) => GetAsync(id, token));
        return await Task.WhenAll(tasks);
    }

    private static void ReleaseLock(TKey id)
    {
        if (_processing.TryGetValue(id, out var mres))
        {
            mres.Set();
            _processing.TryRemove(id, out _);
        }
    }

    // Wrapper to handle single fetch errors
    protected static async Task<TDomainType> FetchInternalAsync(TKey id)
    {
        try
        {
            return await TDerived.FetchSingleAsync(id);
        }
        catch (Exception ex)
        {
            await Console.Out.WriteLineAsync($"Error fetching single {typeof(TDomainType).Name}: {ex}");
            // Depending on logic, you might want to remove the failed entry from _cache so it can be retried
            _cache.TryRemove(id, out _); 
            throw;
        }
    }

    // Wrapper to handle batch fetch errors
    protected static async Task<IEnumerable<TDomainType>> FetchInternalAsync(IEnumerable<TKey> ids)
    {
        try
        {
            return await TDerived.FetchMultipleAsync(ids);
        }
        catch (Exception ex)
        {
            await Console.Out.WriteLineAsync($"Error fetching batch {typeof(TDomainType).Name}: {ex}");
            throw;
        }
    }

    public static Task<bool> ClearSingle(TKey id)
    {
        return Task.FromResult(_cache.TryRemove(id, out _));
    }

    public static Task Clear()
    {
        _cache.Clear();
        return Task.CompletedTask;
    }
}