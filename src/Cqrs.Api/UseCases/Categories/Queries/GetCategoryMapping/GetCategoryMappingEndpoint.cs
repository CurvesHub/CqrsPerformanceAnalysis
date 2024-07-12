using System.Net;
using Cqrs.Api.Common.BaseRequests;
using Cqrs.Api.Common.Constants;
using Cqrs.Api.Common.Endpoints;
using Cqrs.Api.Common.ErrorHandling;
using Microsoft.AspNetCore.Mvc;

namespace Cqrs.Api.UseCases.Categories.Queries.GetCategoryMapping;

/// <inheritdoc />
public class GetCategoryMappingEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("categories", GetCategoryMappingAsync)
            .WithTags(EndpointTags.CATEGORIES)
            .WithSummary("Returns the mapped and the default (german) category of the article based on the request.")
            .Produces<GetCategoryMappingResponse>()
            .ProducesProblem((int)HttpStatusCode.NotFound)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .AddEndpointFilter<ValidationFilter<BaseQuery>>()
            .WithOpenApi();
    }

    private static async Task<IResult> GetCategoryMappingAsync(
        [AsParameters] BaseQuery query,
        [FromServices] GetCategoryMappingQueryHandler queryHandler,
        [FromServices] HttpProblemDetailsService problemDetailsService)
    {
        var result = await queryHandler.GetCategoryMappingAsync(query);

        return result.Match(
            Results.Ok,
            problemDetailsService.LogErrorsAndReturnProblem);
    }
}
