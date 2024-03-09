using System.Net;
using Microsoft.AspNetCore.Mvc;
using Traditional.Api.Common.Constants;
using Traditional.Api.Common.Endpoints;
using Traditional.Api.Common.ErrorHandling;

namespace Traditional.Api.UseCases.Attributes.UpdateAttributeValues;

/// <inheritdoc />
public class UpdateAttributeValuesEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPut("attributes", UpdateAttributeValuesAsync)
            .WithTags(EndpointTags.ATTRIBUTES)
            .WithSummary("Updates the category specific attributes of an article.")
            .Accepts<UpdateAttributeValuesRequest>(isOptional: false, contentType: "application/json")
            .Produces((int)HttpStatusCode.NoContent)
            .ProducesProblem((int)HttpStatusCode.NotFound)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .AddEndpointFilter<ValidationFilter<UpdateAttributeValuesRequest>>()
            .WithOpenApi();
    }

    private static async Task<IResult> UpdateAttributeValuesAsync(
        [FromBody] UpdateAttributeValuesRequest request,
        [FromServices] UpdateAttributeValuesHandler handler,
        [FromServices] HttpProblemDetailsService problemDetailsService)
    {
        var result = await handler.UpdateAttributeValuesAsync(request);

        return result.Match(
            _ => Results.NoContent(),
            problemDetailsService.LogErrorsAndReturnProblem);
    }
}
