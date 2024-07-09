using Cqrs.Api.Common.BaseRequests;
using JetBrains.Annotations;

namespace Cqrs.Api.UseCases.Categories.Commands.UpdateCategoryMapping;

/// <summary>
/// Represents the request for updating the category mappings for an article.
/// </summary>
/// <param name="RootCategoryId">The id of the root category, indicating which category tree to use.</param>
/// <param name="ArticleNumber">The article number for which the category mappings should be updated.</param>
/// <param name="CategoryNumber">The category number to use for the update.</param>
[PublicAPI]
public record UpdateCategoryMappingCommand(
    int RootCategoryId,
    string ArticleNumber,
    long CategoryNumber)
    : BaseRequest(RootCategoryId, ArticleNumber);
