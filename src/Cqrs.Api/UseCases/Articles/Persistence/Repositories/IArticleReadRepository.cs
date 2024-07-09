using Cqrs.Api.UseCases.Articles.Persistence.Entities;
using Cqrs.Api.UseCases.Attributes.Common.Models;

namespace Cqrs.Api.UseCases.Articles.Persistence.Repositories;

/// <summary>
/// Handles the data access for the articles.
/// </summary>
public interface IArticleReadRepository
{
    /// <summary>
    /// Gets the first article by the article number with all associated categories.
    /// </summary>
    /// <param name="articleNumber">The article number to search for.</param>
    /// <returns>A <see cref="Article"/> with all associated categories.</returns>
    public Task<Article?> GetFirstByNumberWithCategories(string articleNumber);

    /// <summary>
    /// Gets the article id with the characteristic id by the article number.
    /// </summary>
    /// <param name="articleNumber">The article number to search for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{ArticleDto}"/> of <see cref="ArticleDto"/>s.</returns>
    IAsyncEnumerable<ArticleDto> GetArticleDtos(string articleNumber);

    /// <summary>
    /// Checks if the article has variants.
    /// </summary>
    /// <param name="articleNumber">The article number to check for.</param>
    /// <returns>A value indicating whether the article has variants.</returns>
    Task<bool> HasArticleVariantsAsync(string articleNumber);
}
