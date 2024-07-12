using Cqrs.Api.Common.BaseRequests;
using Cqrs.Api.UseCases.Attributes.Common.Responses;
using ErrorOr;
using MediatR;

namespace Cqrs.Api.UseCases.Attributes.Queries.GetSubAttributes;

/// <summary>
/// Represents the request to get category specific sub attributes.
/// </summary>
/// <param name="RootCategoryId">Gets the id of the requested category tree.</param>
/// <param name="ArticleNumber">Gets the requested article number.</param>
/// <param name="AttributeIds">Gets the ids of the requested attributes, separated by comma.</param>
public record GetSubAttributesQuery(
    int RootCategoryId,
    string ArticleNumber,
    string AttributeIds)
    : BaseQuery(RootCategoryId, ArticleNumber), IRequest<ErrorOr<List<GetAttributesResponse>>>;
