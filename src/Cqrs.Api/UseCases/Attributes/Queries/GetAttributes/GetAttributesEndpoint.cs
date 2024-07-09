using System.Net;
using Cqrs.Api.Common.BaseRequests;
using Cqrs.Api.Common.Constants;
using Cqrs.Api.Common.Endpoints;
using Cqrs.Api.Common.ErrorHandling;
using Cqrs.Api.UseCases.Attributes.Common.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Cqrs.Api.UseCases.Attributes.Queries.GetAttributes;

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
            .AddEndpointFilter<ValidationFilter<BaseQuery>>()
            .WithOpenApi();
    }

    private static async Task<IResult> GetAttributesAsync(
        [AsParameters] BaseQuery query,
        [FromServices] GetAttributesQueryHandler queryHandler,
        [FromServices] HttpProblemDetailsService problemDetailsService)
    {
        var result = await queryHandler.GetAttributesAsync(query);

        return result.Match(
            responses => Results.Ok(responses.OrderBy(response => response.MinValues)),
            problemDetailsService.LogErrorsAndReturnProblem);
    }
}
