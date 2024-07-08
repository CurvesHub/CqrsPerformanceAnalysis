using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.UseCases.Articles.Persistence.Entities;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using Cqrs.Tests.TestCommon.Factories;
using Microsoft.EntityFrameworkCore;
using TestCommon.Constants;
using Attribute = Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Cqrs.Tests.UseCases.Attributes.Common;

/// <summary>
/// Provides test data for the attribute endpoint tests.
/// </summary>
public static class AttributeTestData
{
    /// <summary>
    /// Creates test data for the attribute endpoint tests.
    /// </summary>
    /// <param name="dbContext">The db context to add the test data to.</param>
    /// <returns>A tuple containing the created category, article, and attribute.</returns>
    public static async Task<(Category category, Article article, Attribute attribute)> CreateTestData(TraditionalDbContext dbContext)
    {
        var germanRootCategory = await dbContext.RootCategories.SingleAsync(rootCategory => rootCategory.Id == TestConstants.RootCategory.GERMAN_ROOT_CATEGORY_ID);
        var category = CategoryFactory.CreateCategory(rootCategory: germanRootCategory);
        var article = ArticleFactory.CreateArticle(categories: [category]);

        var attribute = AttributeFactory.CreateAttribute(
            name: "PRODUCT",
            valueType: AttributeValueType.Boolean,
            minValues: 1,
            maxValues: 1,
            marketplaceAttributeIds: "PRODUCT" + AttributeValueType.Boolean + 1,
            rootCategory: germanRootCategory,
            categories: [category]);

        await dbContext.Categories.AddAsync(category);
        await dbContext.Articles.AddAsync(article);
        await dbContext.Attributes.AddAsync(attribute);

        return (category, article, attribute);
    }

    /// <summary>
    /// Creates test data for the attribute endpoint tests with sub and leaf attributes.
    /// </summary>
    /// <param name="dbContext">The db context to add the test data to.</param>
    /// <returns>A tuple containing the created category, article, attribute, sub attribute and leaf attributes.</returns>
    public static async Task<(Category category, Article article, Attribute attribute, Attribute subAttribute)> CreateTestDataWithSubAndLeafAttributes(TraditionalDbContext dbContext)
    {
        var (category, article, attribute) = await CreateTestData(dbContext);

        var subAttribute = AttributeFactory.AddSubAttributesTo(attribute, 1, AttributeValueType.Boolean).Single();

        await dbContext.Attributes.AddAsync(subAttribute);

        return (category, article, attribute, subAttribute);
    }
}
