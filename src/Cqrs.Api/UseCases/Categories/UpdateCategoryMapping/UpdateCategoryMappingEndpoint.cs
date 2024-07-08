using System.Net;
using Cqrs.Api.Common.Constants;
using Cqrs.Api.Common.Endpoints;
using Cqrs.Api.Common.ErrorHandling;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using Microsoft.AspNetCore.Mvc;

namespace Cqrs.Api.UseCases.Categories.UpdateCategoryMapping;

/// <inheritdoc />
public class UpdateCategoryMappingEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapPut("categories", UpdateCategoryMappingAsync)
            .WithTags(EndpointTags.CATEGORIES)
            .WithSummary("Updates the category mapping of an article and returns the new associated category.")
            .Accepts<UpdateCategoryMappingRequest>(isOptional: false, contentType: "application/json")
            .Produces<UpdateCategoryMappingResponse>((int)HttpStatusCode.Created)
            .ProducesProblem((int)HttpStatusCode.NotFound)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .AddEndpointFilter<ValidationFilter<UpdateCategoryMappingRequest>>()
            .WithOpenApi();
    }

    private static async Task<IResult> UpdateCategoryMappingAsync(
        [FromBody] UpdateCategoryMappingRequest request,
        [FromServices] UpdateCategoryMappingHandler handler,
        [FromServices] HttpProblemDetailsService problemDetailsService)
    {
        var result = await handler.UpdateCategoryMappingAsync(request);

        return result.Match(
            category => Results.Created("categories", ToResponse(category)),
            problemDetailsService.LogErrorsAndReturnProblem);
    }

    private static UpdateCategoryMappingResponse ToResponse(Category category)
    {
        return new UpdateCategoryMappingResponse(
                category.CategoryNumber,
                category.Path);
    }
}
