using JetBrains.Annotations;

namespace Cqrs.Api.UseCases.Categories.Queries.GetChildrenOrTopLevel;

/// <summary>
/// Represents the response to get children of a category.
/// </summary>
/// <param name="CategoryNumber">The category number of the child.</param>
/// <param name="Label">The category path or name to be displayed to the user.</param>
/// <param name="IsSelected">A bool indicating whether the node is the selected category.</param>
/// <param name="IsLeaf">A bool indicating whether the node is a leaf node.</param>
[PublicAPI]
public record GetChildrenOrTopLevelResponse(
    long CategoryNumber,
    string Label,
    bool IsSelected,
    bool IsLeaf);
