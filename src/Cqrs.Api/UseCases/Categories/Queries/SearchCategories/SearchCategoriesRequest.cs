using Cqrs.Api.Common.BaseRequests;
using JetBrains.Annotations;

namespace Cqrs.Api.UseCases.Categories.Queries.SearchCategories;

/// <summary>
/// Represents the request for the category search.
/// </summary>
/// <param name="RootCategoryId">The requested category tree id.</param>
/// <param name="ArticleNumber">The requested article number.</param>
/// <param name="CategoryNumber">The requested category number, if any is specified.</param>
/// <param name="SearchTerm">The requested case-insensitive search term, if any is specified.</param>
[PublicAPI]
public record SearchCategoriesRequest(
    int RootCategoryId,
    string ArticleNumber,
    long? CategoryNumber = null,
    string? SearchTerm = null)
    : BaseRequest(RootCategoryId, ArticleNumber);
