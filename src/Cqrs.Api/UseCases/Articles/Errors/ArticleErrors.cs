using System.Globalization;
using ErrorOr;

namespace Cqrs.Api.UseCases.Articles.Errors;

/// <summary>
/// Defines the article errors.
/// </summary>
public static class ArticleErrors
{
    /// <summary>
    /// Produces an error when the article could not be found.
    /// </summary>
    /// <param name="articleNumber">The article number which could not be found.</param>
    /// <returns>A not found <see cref="ErrorOr.Error"/>.</returns>
    public static Error ArticleNotFound(string articleNumber)
        => Error.NotFound(
            code: "ArticleNotFound",
            description: $"Article with number '{articleNumber}' could not be found.");

    /// <summary>
    /// Produces an error when the article is not mapped to a category.
    /// </summary>
    /// <param name="articleNumber">The article number which has no mapped category on the root category.</param>
    /// <param name="rootCategoryId">The root category id where the article has no mapped category.</param>
    /// <returns>A not found <see cref="ErrorOr.Error"/>.</returns>
    public static Error MappedCategoriesForArticleNotFound(string articleNumber, int rootCategoryId)
        => Error.NotFound(
            code: "MappedCategoriesForArticleNotFound",
            description: $"Article with article number '{articleNumber}' has currently no mapped category for root category id '{rootCategoryId.ToString(CultureInfo.InvariantCulture)}'.");
}
