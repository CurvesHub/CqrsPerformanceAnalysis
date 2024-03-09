using JetBrains.Annotations;
using Traditional.Api.Common.BaseRequests;

namespace Traditional.Api.UseCases.Categories.GetChildrenOrTopLevel;

/// <summary>
/// Represents the request to get the children of a category or the top level categories.
/// </summary>
/// <param name="RootCategoryId">The id of the requested category root category.</param>
/// <param name="ArticleNumber">The requested article number.</param>
/// <param name="CategoryNumber">The category number of the requested category, if any is specified.</param>
[PublicAPI]
public record GetChildrenOrTopLevelRequest(
    int RootCategoryId,
    string ArticleNumber,
    long? CategoryNumber)
    : BaseRequest(RootCategoryId, ArticleNumber);
