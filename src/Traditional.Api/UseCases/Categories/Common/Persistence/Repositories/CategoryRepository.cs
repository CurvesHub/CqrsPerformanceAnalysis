using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Traditional.Api.Common.DataAccess.Persistence;
using Traditional.Api.UseCases.Categories.Common.Persistence.Entities;

namespace Traditional.Api.UseCases.Categories.Common.Persistence.Repositories;

/// <inheritdoc />
[SuppressMessage("Performance", "MA0020:Use direct methods instead of LINQ methods", Justification = "Not possible with EF Core linq queries")]
internal class CategoryRepository(TraditionalDbContext _dbContext) : ICategoryRepository
{
    /// <inheritdoc />
    public async Task<Category?> GetMappedCategoryByRootCategoryId(string articleNumber, int rootCategoryId)
    {
        return await _dbContext.Categories
            .SingleOrDefaultAsync(category =>
                category.RootCategoryId == rootCategoryId
                && category.Articles!.Any(a => a.ArticleNumber == articleNumber));
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
