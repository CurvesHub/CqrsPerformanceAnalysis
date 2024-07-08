using System.Net;
using Cqrs.Api.Common.Constants;
using Cqrs.Api.Common.Endpoints;
using Cqrs.Api.Common.ErrorHandling;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Cqrs.Api.UseCases.Categories.GetChildrenOrTopLevel;

/// <inheritdoc />
public class GetChildrenOrTopLevelEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("categories/children", GetChildrenAsync)
            .WithTags(EndpointTags.CATEGORIES)
            .WithSummary("Returns either the children of the requested category or the top level categories if no category number is specified.")
            .WithDescription(
                "The list of categories includes only children of the specified category NOT their children. " +
                "It is sorted in ascending order by the label of the category.")
            .Produces<IEnumerable<GetChildrenOrTopLevelResponse>>()
            .ProducesProblem((int)HttpStatusCode.NotFound)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .AddEndpointFilter<ValidationFilter<GetChildrenOrTopLevelRequest>>()
            .WithOpenApi();
    }

    private static async Task<IResult> GetChildrenAsync(
        [AsParameters] GetChildrenOrTopLevelRequest request,
        [FromServices] GetChildrenOrTopLevelHandler handler,
        [FromServices] HttpProblemDetailsService problemDetailsService)
    {
        var result = await handler.GetChildrenAsync(request);

        return result.Match(
            categories => Results.Ok(ToResponse(categories)),
            problemDetailsService.LogErrorsAndReturnProblem);
    }

    private static IEnumerable<GetChildrenOrTopLevelResponse> ToResponse(IEnumerable<Category> categories)
    {
        return categories
            .Select(category =>
                new GetChildrenOrTopLevelResponse(
                    category.CategoryNumber,
                    category.Name,
                    category.IsSelected,
                    category.IsLeaf))
            .OrderBy(category => category.Label, StringComparer.OrdinalIgnoreCase);
    }
}
