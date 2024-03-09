using ErrorOr;
using Traditional.Api.Common.Interfaces;
using Traditional.Api.UseCases.Articles.Errors;
using Traditional.Api.UseCases.Articles.Persistence.Entities;
using Traditional.Api.UseCases.Categories.Common.Errors;
using Traditional.Api.UseCases.Categories.Common.Persistence.Entities;

namespace Traditional.Api.UseCases.Categories.UpdateCategoryMapping;

/// <summary>
/// Provides functionality to update the category mapping for an article.
/// </summary>
public class UpdateCategoryMappingHandler(
    ICategoryRepository _categoryRepository,
    IArticleRepository _articleRepository)
{
    /// <summary>
    /// Updates the category mapping for an article.
    /// </summary>
    /// <param name="request">Provides the information for which category mapping should be updated.</param>
    /// <returns>An <see cref="ErrorOr.Error"/> or the new mapped <see cref="Category"/> of the article.</returns>
    public async Task<ErrorOr<Category>> UpdateCategoryMappingAsync(UpdateCategoryMappingRequest request)
    {
        // 1. Retrieve the requested article including all variants and the associated categories for the requested rootCategoryId
        var articles = await _articleRepository.GetByNumberWithCategoriesByRootCategoryId(
                request.ArticleNumber,
                request.RootCategoryId)
            .ToListAsync();

        // If no articles are found return a not found error because no update is possible
        if (articles.Count is 0)
        {
            return ArticleErrors.ArticleNotFound(request.ArticleNumber);
        }

        // 2. Retrieve the requested category
        var category = await _categoryRepository
            .GetByNumberAndRootCategoryId(request.RootCategoryId, request.CategoryNumber);

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

        await _articleRepository.SaveChangesAsync();
    }
}
