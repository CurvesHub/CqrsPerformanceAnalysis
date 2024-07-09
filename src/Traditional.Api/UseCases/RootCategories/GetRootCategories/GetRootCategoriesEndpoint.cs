using System.Net;
using Traditional.Api.Common.Constants;
using Traditional.Api.Common.DataAccess.Repositories;
using Traditional.Api.Common.Endpoints;
using Traditional.Api.UseCases.RootCategories.Common.Persistence.Entities;

namespace Traditional.Api.UseCases.RootCategories.GetRootCategories;

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

    private static async Task<IResult> GetRootCategoriesAsync(ICachedRepository<RootCategory> _rootCategoryRepository)
    {
        var rootCategories = await _rootCategoryRepository.GetAllAsync();

        var response = rootCategories.Select(rootCategory =>
            new GetRootCategoryResponse(
                rootCategory.Id,
                rootCategory.LocaleCode,
                rootCategory.LocaleCode is LocaleCode.de_DE));

        return Results.Ok(response);
    }
}
