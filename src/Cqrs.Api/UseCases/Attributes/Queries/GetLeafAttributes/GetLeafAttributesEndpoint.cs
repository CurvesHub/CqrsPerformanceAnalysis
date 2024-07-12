using System.Net;
using Cqrs.Api.Common.Constants;
using Cqrs.Api.Common.Endpoints;
using Cqrs.Api.Common.ErrorHandling;
using Cqrs.Api.UseCases.Attributes.Common.Responses;
using Microsoft.AspNetCore.Mvc;

namespace Cqrs.Api.UseCases.Attributes.Queries.GetLeafAttributes;

/// <inheritdoc />
public class GetLeafAttributesEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("attributes/leafAttributes", GetLeafAttributesAsync)
            .WithTags(EndpointTags.ATTRIBUTES)
            .WithSummary("Returns a list of category specific leaf attributes based on the request.")
            .Produces<IEnumerable<GetAttributesResponse>>()
            .ProducesProblem((int)HttpStatusCode.NotFound)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .AddEndpointFilter<ValidationFilter<GetLeafAttributesQuery>>()
            .WithOpenApi();
    }

    private static async Task<IResult> GetLeafAttributesAsync(
        [AsParameters] GetLeafAttributesQuery query,
        [FromServices] GetLeafAttributesQueryHandler queryHandler,
        [FromServices] HttpProblemDetailsService problemDetailsService)
    {
        var result = await queryHandler.GetLeafAttributesAsync(query);

        return result.Match(
            Results.Ok,
            problemDetailsService.LogErrorsAndReturnProblem);
    }
}
