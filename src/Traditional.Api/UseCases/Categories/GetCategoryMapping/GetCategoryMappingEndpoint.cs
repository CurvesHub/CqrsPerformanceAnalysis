using System.Net;
using Microsoft.AspNetCore.Mvc;
using Traditional.Api.Common.BaseRequests;
using Traditional.Api.Common.Constants;
using Traditional.Api.Common.Endpoints;
using Traditional.Api.Common.ErrorHandling;

namespace Traditional.Api.UseCases.Categories.GetCategoryMapping;

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
            .AddEndpointFilter<ValidationFilter<BaseRequest>>()
            .WithOpenApi();
    }

    private static async Task<IResult> GetCategoryMappingAsync(
        [AsParameters] BaseRequest request,
        [FromServices] GetCategoryMappingHandler handler,
        [FromServices] HttpProblemDetailsService problemDetailsService)
    {
        var result = await handler.GetCategoryMappingAsync(request);

        return result.Match(
            Results.Ok,
            problemDetailsService.LogErrorsAndReturnProblem);
    }
}
