using JetBrains.Annotations;

namespace Traditional.Api.UseCases.Categories.SearchCategories;

/// <summary>
/// Represents a category tree with its children.
/// </summary>
/// <param name="CategoryNumber">The category number of the current category node.</param>
/// <param name="Label">The category path or name to be displayed to the user.</param>
/// <param name="IsSelected">A bool indicating whether the node is the selected category.</param>
/// <param name="IsLeaf">A bool indicating whether the node is a leaf node.</param>
[PublicAPI]
public record SearchCategoriesResponse(
    long CategoryNumber,
    string Label,
    bool IsSelected,
    bool IsLeaf)
{
    /// <summary>
    /// Gets or sets the children of the node.
    /// </summary>
    public IEnumerable<SearchCategoriesResponse> Children { get; set; } = [];
}
