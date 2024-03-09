using JetBrains.Annotations;
using Traditional.Api.Common.BaseRequests;

namespace Traditional.Api.UseCases.Categories.UpdateCategoryMapping;

/// <summary>
/// Represents the request for updating the category mappings for an article.
/// </summary>
/// <param name="RootCategoryId">The id of the root category, indicating which category tree to use.</param>
/// <param name="ArticleNumber">The article number for which the category mappings should be updated.</param>
/// <param name="CategoryNumber">The category number to use for the update.</param>
[PublicAPI]
public record UpdateCategoryMappingRequest(
    int RootCategoryId,
    string ArticleNumber,
    long CategoryNumber)
    : BaseRequest(RootCategoryId, ArticleNumber);
