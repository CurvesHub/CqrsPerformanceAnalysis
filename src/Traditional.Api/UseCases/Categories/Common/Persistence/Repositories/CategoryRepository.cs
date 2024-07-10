using Microsoft.EntityFrameworkCore;
using Traditional.Api.Common.DataAccess.Persistence;
using Traditional.Api.UseCases.Categories.Common.Persistence.Entities;

namespace Traditional.Api.UseCases.Categories.Common.Persistence.Repositories;

/// <inheritdoc />
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
}
