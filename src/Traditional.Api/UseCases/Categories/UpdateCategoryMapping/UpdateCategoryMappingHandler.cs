using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Traditional.Api.Common.DataAccess.Persistence;
using Traditional.Api.UseCases.Articles.Errors;
using Traditional.Api.UseCases.Articles.Persistence.Entities;
using Traditional.Api.UseCases.Categories.Common.Errors;
using Traditional.Api.UseCases.Categories.Common.Persistence.Entities;

namespace Traditional.Api.UseCases.Categories.UpdateCategoryMapping;

/// <summary>
/// Provides functionality to update the category mapping for an article.
/// </summary>
public class UpdateCategoryMappingHandler(TraditionalDbContext _dbContext)
{
    /// <summary>
    /// Updates the category mapping for an article.
    /// </summary>
    /// <param name="request">Provides the information for which category mapping should be updated.</param>
    /// <returns>An <see cref="ErrorOr.Error"/> or the new mapped <see cref="Category"/> of the article.</returns>
    public async Task<ErrorOr<Category>> UpdateCategoryMappingAsync(UpdateCategoryMappingRequest request)
    {
        // 1. Retrieve the requested article including all variants and the associated categories for the requested rootCategoryId
        var articles = await GetByNumberWithCategoriesByRootCategoryId(
                request.ArticleNumber,
                request.RootCategoryId)
            .ToListAsync();

        // If no articles are found return a not found error because no update is possible
        if (articles.Count is 0)
        {
            return ArticleErrors.ArticleNotFound(request.ArticleNumber);
        }

        // 2. Retrieve the requested category
        var category = await GetByNumberAndRootCategoryId(request.RootCategoryId, request.CategoryNumber);

        // If no category was found return a not found error
        if (category is null)
        {
            return CategoryErrors.CategoryNotFound(request.CategoryNumber, request.RootCategoryId);
        }

        // 3. Update the category mapping for the articles and return the new associated category
        await UpdateCategoryMappingForArticlesAsync(articles, category, request.RootCategoryId);

        return category;
    }

    private async Task UpdateCategoryMappingForArticlesAsync(
        List<Article> articles,
        Category newCategory,
        int rootCategoryId)
    {
        foreach (var categories in articles.Select(a => a.Categories))
        {
            // An article can have multiple categories on different roots but only one per root
            categories!.RemoveAll(category => category.RootCategoryId == rootCategoryId);
            categories.Add(newCategory);
        }

        await _dbContext.SaveChangesAsync();
    }

    /// <summary>
    /// Gets the articles by the article number with the categories by the root category id.
    /// </summary>
    /// <param name="articleNumber">The article number to search for.</param>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{Article}"/> of <see cref="Article"/>s.</returns>
    private IAsyncEnumerable<Article> GetByNumberWithCategoriesByRootCategoryId(string articleNumber, int rootCategoryId)
    {
        return _dbContext.Articles
            .Where(article => article.ArticleNumber == articleNumber)
            .Include(article => article.Categories!
                .Where(category => category.RootCategoryId == rootCategoryId))
            .AsAsyncEnumerable();
    }

    /// <summary>
    /// Gets the categories by the category number and the root category id.
    /// </summary>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <param name="categoryNumber">The category number to search for.</param>
    /// <returns>A <see cref="Category"/> or <see langword="null"/> if not found.</returns>
    private async Task<Category?> GetByNumberAndRootCategoryId(int rootCategoryId, long categoryNumber)
    {
        return await _dbContext.Categories
            .SingleOrDefaultAsync(category =>
                category.RootCategoryId == rootCategoryId
                && category.CategoryNumber == categoryNumber);
    }
}
