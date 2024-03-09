using Microsoft.EntityFrameworkCore;
using Traditional.Api.Common.DataAccess.Entities;
using Traditional.Api.Common.DataAccess.Persistence;
using Traditional.Api.Common.Exceptions;
using Traditional.Api.Common.Interfaces;

namespace Traditional.Api.Common.DataAccess.Repositories;

/// <inheritdoc />
internal class CachedRepository<TItem>(
    TraditionalDbContext _dbContext,
    Cache<TItem> _cache)
    : ICachedRepository<TItem>
    where TItem : BaseEntity
{
    /// <inheritdoc />
    public async Task<List<TItem>> GetAllAsync()
    {
        return await _cache.GetOrSetAllAsync(RetrieveItemsAsync);
    }

    /// <inheritdoc />
    public async Task<TItem?> GetByIdAsync(int id)
    {
        return (await GetAllAsync())
            .Find(item => item.Id == id);
    }

    private async Task<List<TItem>> RetrieveItemsAsync()
    {
        var items = await _dbContext.Set<TItem>().ToListAsync();

        return items.Count > 0
            ? items
            : throw new SeededTableIsEmptyException(typeof(TItem).Name);
    }
}
