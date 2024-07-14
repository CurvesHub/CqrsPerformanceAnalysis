using Cqrs.Api.Common.BaseRequests;
using ErrorOr;
using JetBrains.Annotations;
using MediatR;

namespace Cqrs.Api.UseCases.Categories.Queries.GetChildrenOrTopLevel;

/// <summary>
/// Represents the request to get the children of a category or the top level categories.
/// </summary>
/// <param name="RootCategoryId">The id of the requested category root category.</param>
/// <param name="ArticleNumber">The requested article number.</param>
/// <param name="CategoryNumber">The category number of the requested category, if any is specified.</param>
[PublicAPI]
public record GetChildrenOrTopLevelQuery(
    int RootCategoryId,
    string ArticleNumber,
    long? CategoryNumber)
    : BaseQuery(RootCategoryId, ArticleNumber), IRequest<ErrorOr<IOrderedEnumerable<GetChildrenOrTopLevelResponse>>>;
