using System.Net;
using Microsoft.AspNetCore.Mvc;
using Traditional.Api.Common.Constants;
using Traditional.Api.Common.Endpoints;
using Traditional.Api.Common.ErrorHandling;
using Traditional.Api.UseCases.Attributes.Common.Responses;

namespace Traditional.Api.UseCases.Attributes.GetSubAttributes;

/// <inheritdoc />
public class GetSubAttributesEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("attributes/subAttributes", GetSubAttributesAsync)
            .WithTags(EndpointTags.ATTRIBUTES)
            .WithSummary("Returns a list of category specific sub attributes ordered by the min values based on the request.")
            .Produces<IEnumerable<GetAttributesResponse>>()
            .ProducesProblem((int)HttpStatusCode.NotFound)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .AddEndpointFilter<ValidationFilter<GetSubAttributesRequest>>()
            .WithOpenApi();
    }

    private static async Task<IResult> GetSubAttributesAsync(
        [AsParameters] GetSubAttributesRequest request,
        [FromServices] GetSubAttributesHandler handler,
        [FromServices] HttpProblemDetailsService problemDetailsService)
    {
        var result = await handler.GetSubAttributesAsync(request);

        return result.Match(
            responses => Results.Ok(responses.OrderByDescending(response => response.MinValues)),
            problemDetailsService.LogErrorsAndReturnProblem);
    }
}
