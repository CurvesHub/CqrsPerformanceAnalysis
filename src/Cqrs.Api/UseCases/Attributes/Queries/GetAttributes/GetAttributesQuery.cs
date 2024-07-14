using Cqrs.Api.Common.BaseRequests;
using Cqrs.Api.UseCases.Attributes.Common.Responses;
using ErrorOr;
using JetBrains.Annotations;
using MediatR;

namespace Cqrs.Api.UseCases.Attributes.Queries.GetAttributes;

/// <summary>
/// Defines common properties for all requests.
/// </summary>
/// <param name="RootCategoryId">The requested root category id.</param>
/// <param name="ArticleNumber">The requested article number.</param>
[PublicAPI]
public record GetAttributesQuery(int RootCategoryId, string ArticleNumber)
    : BaseQuery(RootCategoryId, ArticleNumber), IRequest<ErrorOr<List<GetAttributesResponse>>>;
