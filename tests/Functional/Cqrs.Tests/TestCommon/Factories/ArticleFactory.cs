using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cqrs.Api.UseCases.Articles.Persistence.Entities;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using TestCommon.Constants;

namespace Cqrs.Tests.TestCommon.Factories;

/// <summary>
/// A factory for creating test <see cref="Article"/> instances.
/// </summary>
[SuppressMessage("Design", "S107: Methods should not have too many parameters", Justification = "Required for testing.")]
public static class ArticleFactory
{
    /// <summary>
    /// Creates an instance of an <see cref="Article"/>.
    /// </summary>
    /// <param name="articleNumber">The article number.</param>
    /// <param name="characteristicId">The characteristic id.</param>
    /// <param name="categories">The associated categories.</param>
    /// <param name="attributeBooleanValues">The associated attribute boolean values.</param>
    /// <param name="attributeDecimalValues">The associated attribute decimal values.</param>
    /// <param name="attributeIntValues">The associated attribute int values.</param>
    /// <param name="attributeStringValues">The associated attribute string values.</param>
    /// <returns>A new instance of <see cref="Article"/>.</returns>
    public static Article CreateArticle(
        string? articleNumber = null,
        int? characteristicId = null,
        List<Category>? categories = null,
        List<AttributeBooleanValue>? attributeBooleanValues = null,
        List<AttributeDecimalValue>? attributeDecimalValues = null,
        List<AttributeIntValue>? attributeIntValues = null,
        List<AttributeStringValue>? attributeStringValues = null)
    {
        return new Article(
            articleNumber ?? TestConstants.Article.ARTILCE_NUMBER,
            characteristicId ?? 0)
        {
            Categories = categories,
            AttributeBooleanValues = attributeBooleanValues,
            AttributeDecimalValues = attributeDecimalValues,
            AttributeIntValues = attributeIntValues,
            AttributeStringValues = attributeStringValues
        };
    }

    /// <summary>
    /// Creates multiple instances of an <see cref="Article"/>.
    /// </summary>
    /// <param name="amount">The amount of articles to create.</param>
    /// <returns>A collection of new <see cref="Article"/> instances.</returns>
    public static IEnumerable<Article> CreateArticles(int amount)
    {
        return Enumerable
            .Range(0, amount)
            .Select(index => CreateArticle(articleNumber: GetNextArticleNumber(index)));
    }

    /// <summary>
    /// Creates multiple variants of the <see cref="Article"/> with <paramref name="articleNumber"/>.
    /// </summary>
    /// <param name="amount">The amount of articles to create.</param>
    /// <param name="articleNumber">The article number.</param>
    /// <param name="categories">The associated categories.</param>
    /// <returns>A collection of new <see cref="Article"/> instances.</returns>
    public static IEnumerable<Article> CreateVariants(int amount, string? articleNumber = null, List<Category>? categories = null)
    {
        return Enumerable
            .Range(0, amount)
            .Select(index => CreateArticle(
                articleNumber: articleNumber,
                characteristicId: index + 1,
                categories: categories));
    }

    /// <summary>
    /// Gets the next <see cref="TestConstants.Article.ARTILCE_NUMBER"/> incremented by the given value.
    /// </summary>
    /// <param name="increment">The increment value.</param>
    /// <returns>A string representing the next article number.</returns>
    private static string GetNextArticleNumber(int increment)
    {
        long next = long.Parse(TestConstants.Article.ARTILCE_NUMBER, CultureInfo.InvariantCulture) + increment;
        return next.ToString(CultureInfo.InvariantCulture);
    }
}
