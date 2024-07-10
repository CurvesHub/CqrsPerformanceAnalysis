using Cqrs.Api.Common.DataAccess.Entities;
using Cqrs.Api.UseCases.Articles.Persistence.Entities;
using Cqrs.Api.UseCases.RootCategories.Common.Persistence.Entities;
using Attribute = Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;

/// <summary>
/// Entity to store categories.
/// </summary>
/// <param name="categoryNumber">Sets the category number.</param>
/// <param name="name">Sets the name of the category.</param>
/// <param name="path">Sets the path of the category.</param>
/// <param name="isLeaf">Sets a value indicating whether the category is a leaf category.</param>
public class Category(
    long categoryNumber,
    string name,
    string path,
    bool isLeaf)
    : BaseEntity
{
    /// <summary>
    /// Gets the category number.
    /// </summary>
    public long CategoryNumber { get; init; } = categoryNumber;

    /// <summary>
    /// Gets the category number off the parent category.
    /// </summary>
    public long? ParentCategoryNumber => Parent?.CategoryNumber;

    /// <summary>
    /// Gets the associated name.
    /// </summary>
    public string Name { get; init; } = name;

    /// <summary>
    /// Gets the associated path.
    /// </summary>
    public string Path { get; init; } = path;

    /// <summary>
    /// Gets a value indicating whether the category is a leaf category.
    /// </summary>
    public bool IsLeaf { get; init; } = isLeaf;

    /// <summary>
    /// Gets or sets the id of the associated <see cref="RootCategory"/>.
    /// </summary>
    public int RootCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the associated <see cref="RootCategory"/>.
    /// </summary>
    public RootCategory? RootCategory { get; set; }

    /// <summary>
    /// Gets or sets the id of the associated parent <see cref="Category"/>.
    /// </summary>
    public int? ParentId { get; set; }

    /// <summary>
    /// Gets or sets the associated parent <see cref="Category"/>.
    /// </summary>
    public Category? Parent { get; set; }

    /// <summary>
    /// Gets or sets a collection of associated child <see cref="Category"/>s.
    /// </summary>
    public List<Category>? Children { get; set; }

    /// <summary>
    /// Gets or sets a collection of associated <see cref="Attribute"/>s.
    /// </summary>
    public List<Attribute>? Attributes { get; set; }

    /// <summary>
    /// Gets or sets a collection of associated <see cref="Article"/>s.
    /// </summary>
    public List<Article>? Articles { get; set; }
}
