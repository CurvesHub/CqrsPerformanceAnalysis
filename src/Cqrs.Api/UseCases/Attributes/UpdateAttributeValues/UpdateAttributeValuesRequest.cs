using Cqrs.Api.Common.BaseRequests;
using JetBrains.Annotations;

namespace Cqrs.Api.UseCases.Attributes.UpdateAttributeValues;

/// <summary>
/// Represents the request to put category specific attributes.
/// </summary>
/// <param name="RootCategoryId">Gets the id of the requested category tree.</param>
/// <param name="ArticleNumber">Gets the requested article number.</param>
/// <param name="NewAttributeValues">Gets the new attribute values that should be assigned to the given article.</param>
[PublicAPI]
public record UpdateAttributeValuesRequest(
    int RootCategoryId,
    string ArticleNumber,
    NewAttributeValue[] NewAttributeValues)
    : BaseRequest(RootCategoryId, ArticleNumber);
