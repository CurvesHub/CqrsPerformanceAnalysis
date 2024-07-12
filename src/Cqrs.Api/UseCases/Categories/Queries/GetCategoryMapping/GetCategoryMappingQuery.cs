using Cqrs.Api.Common.BaseRequests;
using ErrorOr;
using JetBrains.Annotations;
using MediatR;

namespace Cqrs.Api.UseCases.Categories.Queries.GetCategoryMapping;

/// <summary>
/// Defines common properties for all requests.
/// </summary>
/// <param name="RootCategoryId">The requested root category id.</param>
/// <param name="ArticleNumber">The requested article number.</param>
[PublicAPI]
public record GetCategoryMappingQuery(int RootCategoryId, string ArticleNumber)
    : BaseQuery(RootCategoryId, ArticleNumber), IRequest<ErrorOr<GetCategoryMappingResponse>>;
