using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.UseCases.Articles.Errors;
using Cqrs.Api.UseCases.Articles.Persistence.Entities;
using Cqrs.Api.UseCases.Categories.Common.Errors;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cqrs.Api.UseCases.Categories.Commands.UpdateCategoryMapping;

/// <summary>
/// Provides functionality to update the category mapping for an article.
/// </summary>
public class UpdateCategoryMappingCommandHandler(CqrsWriteDbContext _dbContext) : IRequestHandler<UpdateCategoryMappingCommand, ErrorOr<Category>>
{
    /// <summary>
    /// Updates the category mapping for an article.
    /// </summary>
    /// <param name="command">Provides the information for which category mapping should be updated.</param>
    /// <param name="cancellationToken">The token to cancel the requests.</param>
    /// <returns>An <see cref="ErrorOr.Error"/> or the new mapped <see cref="Category"/> of the article.</returns>
    public async Task<ErrorOr<Category>> Handle(UpdateCategoryMappingCommand command, CancellationToken cancellationToken)
    {
        // 1. Retrieve the requested article including all variants and the associated categories for the requested rootCategoryId
        var articles = await GetByNumberWithCategoriesByRootCategoryId(
                command.ArticleNumber,
                command.RootCategoryId)
            .ToListAsync();

        // If no articles are found return a not found error because no update is possible
        if (articles.Count is 0)
        {
            return ArticleErrors.ArticleNotFound(command.ArticleNumber);
        }

        // 2. Retrieve the requested category
        var category = await GetByNumberAndRootCategoryId(command.RootCategoryId, command.CategoryNumber);

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
