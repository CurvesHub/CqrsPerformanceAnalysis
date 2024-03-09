using Traditional.Api.UseCases.Articles.Persistence.Entities;
using Traditional.Api.UseCases.Attributes.Common.Models;

namespace Traditional.Api.Common.Interfaces;

/// <summary>
/// Handles the data access for the articles.
/// </summary>
public interface IArticleRepository
{
    /// <summary>
    /// Gets the articles by the article number with the categories by the root category id.
    /// </summary>
    /// <param name="articleNumber">The article number to search for.</param>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{Article}"/> of <see cref="Article"/>s.</returns>
    public IAsyncEnumerable<Article> GetByNumberWithCategoriesByRootCategoryId(string articleNumber, int rootCategoryId);

    /// <summary>
    /// Gets the first article by the article number with all associated categories.
    /// </summary>
    /// <param name="articleNumber">The article number to search for.</param>
    /// <returns>A <see cref="Article"/> with all associated categories.</returns>
    public Task<Article?> GetFirstByNumberWithCategories(string articleNumber);

    /// <summary>
    /// Saves the changes to the database.
    /// </summary>
    /// <param name="cancellationToken">A <see cref="CancellationToken"/> to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Gets the articles by the article number with all attribute values.
    /// </summary>
    /// <param name="articleNumber">The article number to search for.</param>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{Article}"/> of <see cref="Article"/>s.</returns>
    public IAsyncEnumerable<Article> GetByNumberWithAttributeValuesByRootCategoryId(string articleNumber, int rootCategoryId);
}
