using Cqrs.Api.Common.BaseRequests;

namespace Cqrs.Api.UseCases.Attributes.GetLeafAttributes;

/// <summary>
/// Represents the request to get category specific leaf attributes.
/// </summary>
/// <param name="RootCategoryId">Gets the id of the requested category tree.</param>
/// <param name="ArticleNumber">Gets the requested article number.</param>
/// <param name="AttributeId">Gets the id of the requested attribute.</param>
public record GetLeafAttributesRequest(
    int RootCategoryId,
    string ArticleNumber,
    string AttributeId)
    : BaseRequest(RootCategoryId, ArticleNumber);
