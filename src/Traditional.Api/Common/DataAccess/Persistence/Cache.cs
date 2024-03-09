using Microsoft.Extensions.Caching.Memory;
using Traditional.Api.Common.DataAccess.Entities;
using Traditional.Api.UseCases.RootCategories.Common.Persistence.Entities;

namespace Traditional.Api.Common.DataAccess.Persistence;

/// <summary>
/// Defines a cache for a specific type.
/// </summary>
/// <typeparam name="TItem">The type of the item to cache.</typeparam>
internal class Cache<TItem>(IMemoryCache _memoryCache)
    where TItem : BaseEntity
{
    private readonly string _key = typeof(TItem).Name;
    private readonly TimeSpan _expirationTime = typeof(TItem) == typeof(RootCategory)
        ? TimeSpan.FromDays(1)
        : TimeSpan.FromHours(1);

    /// <summary>
    /// Gets or sets all items if they are not already in the cache.
    /// </summary>
    /// <param name="factory">The factory to retrieve the items.</param>
    /// <returns>A list of items.</returns>
    public async Task<List<TItem>> GetOrSetAllAsync(Func<Task<List<TItem>>> factory)
    {
        return _memoryCache.Get<List<TItem>>(_key) ??
               _memoryCache.Set(_key, await factory(), _expirationTime);
    }
}
