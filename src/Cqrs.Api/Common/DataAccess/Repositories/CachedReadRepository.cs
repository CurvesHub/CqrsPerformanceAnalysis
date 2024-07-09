using Cqrs.Api.Common.DataAccess.Entities;
using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.Common.Exceptions;
using Cqrs.Api.Common.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace Cqrs.Api.Common.DataAccess.Repositories;

/// <inheritdoc />
internal class CachedReadRepository<TItem>(
    CqrsReadDbContext _dbContext,
    Cache<TItem> _cache)
    : ICachedReadRepository<TItem>
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
