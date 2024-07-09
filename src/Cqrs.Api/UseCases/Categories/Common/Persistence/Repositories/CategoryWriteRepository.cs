using System.Diagnostics.CodeAnalysis;
using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cqrs.Api.UseCases.Categories.Common.Persistence.Repositories;

/// <inheritdoc />
[SuppressMessage("Performance", "MA0020:Use direct methods instead of LINQ methods", Justification = "Not possible with EF Core linq queries")]
internal class CategoryWriteRepository(CqrsWriteDbContext _dbContext) : ICategoryWriteRepository
{
    /// <inheritdoc />
    public async Task<Category?> GetByNumberAndRootCategoryId(int rootCategoryId, long categoryNumber)
    {
        return await _dbContext.Categories
            .SingleOrDefaultAsync(category =>
                category.RootCategoryId == rootCategoryId
                && category.CategoryNumber == categoryNumber);
    }

    /// <inheritdoc />
    public async Task<int?> GetMappedCategoryIdByRootCategoryId(string articleNumber, int rootCategoryId)
    {
        return await _dbContext.Categories
            .Where(category => category.RootCategoryId == rootCategoryId && category.Articles!.Any(article => article.ArticleNumber == articleNumber))
            .Select(category => (int?)category.Id)
            .SingleOrDefaultAsync();
    }
}
