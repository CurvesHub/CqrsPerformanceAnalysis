using System.Net;
using Cqrs.Api.Common.Constants;
using Cqrs.Api.Common.Endpoints;
using Cqrs.Api.Common.ErrorHandling;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cqrs.Api.UseCases.Categories.Queries.GetChildrenOrTopLevel;

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
            .AddEndpointFilter<ValidationFilter<GetChildrenOrTopLevelQuery>>()
            .WithOpenApi();
    }

    private static async Task<IResult> GetChildrenAsync(
        [AsParameters] GetChildrenOrTopLevelQuery query,
        [FromServices] ISender sender,
        [FromServices] HttpProblemDetailsService problemDetailsService)
    {
        var result = await sender.Send(query);

        return result.Match(
            Results.Ok,
            problemDetailsService.LogErrorsAndReturnProblem);
    }
}
