using System.Net;
using Cqrs.Api.Common.Constants;
using Cqrs.Api.Common.Endpoints;
using Cqrs.Api.UseCases.RootCategories.Common.Persistence.Entities;

namespace Cqrs.Api.UseCases.RootCategories.GetRootCategories;

/// <inheritdoc />
public class GetRootCategoriesEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("rootCategories", GetRootCategoriesAsync)
            .WithTags(EndpointTags.ROOT_CATEGORIES)
            .WithSummary("Returns a list of all valid root categories without their children.")
            .Produces<IEnumerable<GetRootCategoryResponse>>()
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .WithOpenApi()
            .CacheOutput(builder => builder.Expire(TimeSpan.FromDays(1)));
    }

    private static async Task<IResult> GetRootCategoriesAsync(GetRootCategoriesHandler handler)
    {
        var rootCategories = await handler.GetRootCategoriesAsync();

        return Results.Ok(ToResponse(rootCategories));
    }

    private static IEnumerable<GetRootCategoryResponse> ToResponse(IEnumerable<RootCategory> rootCategories)
    {
        return rootCategories.Select(rootCategory =>
            new GetRootCategoryResponse(
                rootCategory.Id,
                rootCategory.LocaleCode,
                rootCategory.LocaleCode is LocaleCode.de_DE));
    }
}
