using System.Net;
using Cqrs.Api.Common.Constants;
using Cqrs.Api.Common.Endpoints;
using Cqrs.Api.Common.ErrorHandling;
using Cqrs.Api.UseCases.Attributes.Common.Responses;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace Cqrs.Api.UseCases.Attributes.Queries.GetSubAttributes;

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
            .AddEndpointFilter<ValidationFilter<GetSubAttributesQuery>>()
            .WithOpenApi();
    }

    private static async Task<IResult> GetSubAttributesAsync(
        [AsParameters] GetSubAttributesQuery query,
        [FromServices] ISender sender,
        [FromServices] HttpProblemDetailsService problemDetailsService)
    {
        var result = await sender.Send(query);

        return result.Match(
            responses => Results.Ok(responses.OrderByDescending(response => response.MinValues)),
            problemDetailsService.LogErrorsAndReturnProblem);
    }
}
