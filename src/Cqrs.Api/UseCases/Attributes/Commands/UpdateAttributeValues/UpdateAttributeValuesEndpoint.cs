using System.Net;
using Cqrs.Api.Common.Constants;
using Cqrs.Api.Common.Endpoints;
using Cqrs.Api.Common.ErrorHandling;
using Microsoft.AspNetCore.Mvc;

namespace Cqrs.Api.UseCases.Attributes.Commands.UpdateAttributeValues;

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
            .Accepts<UpdateAttributeValuesCommand>(isOptional: false, contentType: "application/json")
            .Produces((int)HttpStatusCode.NoContent)
            .ProducesProblem((int)HttpStatusCode.NotFound)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .AddEndpointFilter<ValidationFilter<UpdateAttributeValuesCommand>>()
            .WithOpenApi();
    }

    private static async Task<IResult> UpdateAttributeValuesAsync(
        [FromBody] UpdateAttributeValuesCommand command,
        [FromServices] UpdateAttributeValuesCommandHandler commandHandler,
        [FromServices] HttpProblemDetailsService problemDetailsService)
    {
        var result = await commandHandler.UpdateAttributeValuesAsync(command);

        return result.Match(
            _ => Results.NoContent(),
            problemDetailsService.LogErrorsAndReturnProblem);
    }
}
