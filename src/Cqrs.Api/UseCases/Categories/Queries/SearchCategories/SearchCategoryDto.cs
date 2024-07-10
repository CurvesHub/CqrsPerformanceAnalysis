namespace Cqrs.Api.UseCases.Categories.Queries.SearchCategories;

/// <summary>
/// Represents a category search result.
/// </summary>
/// <param name="CategoryNumber">The category number of the current category node.</param>
/// <param name="Label">The category path or name to be displayed to the user.</param>
/// <param name="IsSelected">A bool indicating whether the node is the selected category.</param>
/// <param name="IsLeaf">A bool indicating whether the node is a leaf node.</param>
/// <param name="ParentCategoryNumber">The parent category number of the current category node.</param>
public record SearchCategoryDto(
    long CategoryNumber,
    string Label,
    bool IsSelected,
    bool IsLeaf,
    long? ParentCategoryNumber)
{
    /// <summary>
    /// Gets the children of the node.
    /// </summary>
    public List<SearchCategoryDto> Children { get; } = [];
}
