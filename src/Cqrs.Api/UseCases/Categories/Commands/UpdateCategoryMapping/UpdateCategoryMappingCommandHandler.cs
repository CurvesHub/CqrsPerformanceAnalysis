using Cqrs.Api.UseCases.Articles.Errors;
using Cqrs.Api.UseCases.Articles.Persistence.Entities;
using Cqrs.Api.UseCases.Articles.Persistence.Repositories;
using Cqrs.Api.UseCases.Categories.Common.Errors;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Repositories;
using ErrorOr;

namespace Cqrs.Api.UseCases.Categories.Commands.UpdateCategoryMapping;

/// <summary>
/// Provides functionality to update the category mapping for an article.
/// </summary>
public class UpdateCategoryMappingCommandHandler(
    ICategoryWriteRepository _categoryWriteRepository,
    IArticleWriteRepository _articleWriteRepository)
{
    /// <summary>
    /// Updates the category mapping for an article.
    /// </summary>
    /// <param name="command">Provides the information for which category mapping should be updated.</param>
    /// <returns>An <see cref="ErrorOr.Error"/> or the new mapped <see cref="Category"/> of the article.</returns>
    public async Task<ErrorOr<Category>> UpdateCategoryMappingAsync(UpdateCategoryMappingCommand command)
    {
        // 1. Retrieve the requested article including all variants and the associated categories for the requested rootCategoryId
        var articles = await _articleWriteRepository.GetByNumberWithCategoriesByRootCategoryId(
                command.ArticleNumber,
                command.RootCategoryId)
            .ToListAsync();

        // If no articles are found return a not found error because no update is possible
        if (articles.Count is 0)
        {
            return ArticleErrors.ArticleNotFound(command.ArticleNumber);
        }

        // 2. Retrieve the requested category
        var category = await _categoryWriteRepository
            .GetByNumberAndRootCategoryId(command.RootCategoryId, command.CategoryNumber);

        // If no category was found return a not found error
        if (category is null)
        {
            return CategoryErrors.CategoryNotFound(command.CategoryNumber, command.RootCategoryId);
        }

        // 3. Update the category mapping for the articles and return the new associated category
        await UpdateCategoryMappingForArticlesAsync(articles, category, command.RootCategoryId);

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

        await _articleWriteRepository.SaveChangesAsync();
    }
}
