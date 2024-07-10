using System.Diagnostics.CodeAnalysis;
using Cqrs.Api.UseCases.Articles.Persistence.Entities;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using Cqrs.Api.UseCases.RootCategories.Common.Persistence.Entities;
using TestCommon.Constants;
using Attribute = Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Cqrs.Tests.TestCommon.Factories;

/// <summary>
/// A factory for creating test <see cref="Category"/> instances.
/// </summary>
[SuppressMessage("Design", "S107: Methods should not have too many parameters", Justification = "Required for testing.")]
public static class CategoryFactory
{
    private static long _index = TestConstants.Category.CATEGORY_NUMBER;

    private static long NextUniqueCategoryNumber => _index == long.MaxValue
        ? throw new InvalidOperationException("Unique index no longer guaranteed")
        : _index++;

    /// <summary>
    /// Creates an instance of an <see cref="Category"/>.
    /// </summary>
    /// <param name="categoryNumber">The category number.</param>
    /// <param name="name">The category's name.</param>
    /// <param name="path">The category's path.</param>
    /// <param name="isLeaf">The category's leaf state.</param>
    /// <param name="rootCategory">The associated root category.</param>
    /// <param name="parent">The associated parent category.</param>
    /// <param name="children">The associated child categories.</param>
    /// <param name="attributes">The associated attributes.</param>
    /// <param name="articles">The associated articles.</param>
    /// <returns>A new instance of <see cref="Category"/>.</returns>
    public static Category CreateCategory(
        long? categoryNumber = null,
        string? name = null,
        string? path = null,
        bool isLeaf = false,
        RootCategory? rootCategory = null,
        Category? parent = null,
        List<Category>? children = null,
        List<Attribute>? attributes = null,
        List<Article>? articles = null)
    {
        var rootCategoryId = rootCategory?.Id ?? 0;
        return new Category(
            categoryNumber ?? NextUniqueCategoryNumber,
            name ?? $"{TestConstants.Category.NAME} {NextUniqueCategoryNumber}",
            path ?? $"{TestConstants.Category.PATH} {NextUniqueCategoryNumber}",
            isLeaf)
        {
            RootCategoryId = rootCategoryId,
            RootCategory = rootCategory,
            ParentId = parent?.Id,
            Parent = parent,
            Children = children,
            Attributes = attributes,
            Articles = articles
        };
    }

    /// <summary>
    /// Creates multiple instances of a <see cref="Category"/>.
    /// </summary>
    /// <param name="amount">The amount of categories to create.</param>
    /// <param name="rootCategory">The associated root category.</param>
    /// <param name="parent">The associated parent category.</param>
    /// <param name="isLeaf">The category's leaf state.</param>
    /// <param name="useUniqueCategoryNumber">Indicating whether to use unique category numbers.</param>
    /// <returns>A collection of new <see cref="Category"/> instances.</returns>
    public static IEnumerable<Category> CreateCategories(
        int amount,
        RootCategory? rootCategory = null,
        Category? parent = null,
        bool isLeaf = false,
        bool useUniqueCategoryNumber = true)
    {
        return Enumerable
            .Range(0, amount)
            .Select(index => CreateCategory(
                categoryNumber: useUniqueCategoryNumber ? null : index,
                name: useUniqueCategoryNumber ? null : $"{TestConstants.Category.NAME} {index}",
                path: useUniqueCategoryNumber ? null : $"{TestConstants.Category.PATH} {index}",
                isLeaf: isLeaf,
                rootCategory: rootCategory,
                parent: parent));
    }
}
