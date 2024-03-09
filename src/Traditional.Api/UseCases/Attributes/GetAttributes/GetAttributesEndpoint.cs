using System.Net;
using Microsoft.AspNetCore.Mvc;
using Traditional.Api.Common.BaseRequests;
using Traditional.Api.Common.Constants;
using Traditional.Api.Common.Endpoints;
using Traditional.Api.Common.ErrorHandling;
using Traditional.Api.UseCases.Attributes.Common.Responses;

namespace Traditional.Api.UseCases.Attributes.GetAttributes;

/// <inheritdoc />
public class GetAttributesEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("attributes", GetAttributesAsync)
            .WithTags(EndpointTags.ATTRIBUTES)
            .WithSummary("Returns a list of category specific attributes ordered by the min values based on the request.")
            .Produces<IEnumerable<GetAttributesResponse>>()
            .ProducesProblem((int)HttpStatusCode.NotFound)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .AddEndpointFilter<ValidationFilter<BaseRequest>>()
            .WithOpenApi();
    }

    private static async Task<IResult> GetAttributesAsync(
        [AsParameters] BaseRequest request,
        [FromServices] GetAttributesHandler handler,
        [FromServices] HttpProblemDetailsService problemDetailsService)
    {
        var result = await handler.GetAttributesAsync(request);

        return result.Match(
            responses => Results.Ok(responses.OrderBy(response => response.MinValues)),
            problemDetailsService.LogErrorsAndReturnProblem);
    }
}
