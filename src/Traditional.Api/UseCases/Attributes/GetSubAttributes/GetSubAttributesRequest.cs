using Traditional.Api.Common.BaseRequests;

namespace Traditional.Api.UseCases.Attributes.GetSubAttributes;

/// <summary>
/// Represents the request to get category specific sub attributes.
/// </summary>
/// <param name="RootCategoryId">Gets the id of the requested category tree.</param>
/// <param name="ArticleNumber">Gets the requested article number.</param>
/// <param name="AttributeIds">Gets the ids of the requested attributes, separated by comma.</param>
public record GetSubAttributesRequest(
    int RootCategoryId,
    string ArticleNumber,
    string AttributeIds)
    : BaseRequest(RootCategoryId, ArticleNumber);
