using System.Net;
using Microsoft.AspNetCore.Mvc;
using Traditional.Api.Common.Constants;
using Traditional.Api.Common.Endpoints;
using Traditional.Api.Common.ErrorHandling;
using Traditional.Api.UseCases.Categories.Common.Persistence.Entities;

namespace Traditional.Api.UseCases.Categories.SearchCategories;

/// <inheritdoc />
public class SearchCategoriesEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("categories/search", SearchCategoriesAsync)
            .WithTags(EndpointTags.CATEGORIES)
            .WithSummary("Returns a list of category tree nodes with all their children based on the search request.")
            .WithDescription(
                "Searches a category tree from bottom to top and returns all matches with the parents up to the root. " +
                "Children or siblings of the match are not returned. " +
                "The list is sorted in ascending order by the label of the category.")
            .Produces<IOrderedEnumerable<SearchCategoriesResponse>>()
            .ProducesProblem((int)HttpStatusCode.NotFound)
            .ProducesProblem((int)HttpStatusCode.BadRequest)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .AddEndpointFilter<ValidationFilter<SearchCategoriesRequest>>()
            .WithOpenApi();
    }

    private static async Task<IResult> SearchCategoriesAsync(
        [AsParameters] SearchCategoriesRequest request,
        [FromServices] SearchCategoriesHandler handler,
        [FromServices] HttpProblemDetailsService problemDetailsService)
    {
        var result = await handler.SearchCategoriesAsync(request);

        return result.Match(
            categories => Results.Ok(ToResponse(categories)),
            problemDetailsService.LogErrorsAndReturnProblem);
    }

    private static IOrderedEnumerable<SearchCategoriesResponse> ToResponse(
        IEnumerable<Category> categories)
    {
        return categories.Select(category =>
            new SearchCategoriesResponse(
                category.CategoryNumber,
                category.Name,
                category.IsSelected,
                category.IsLeaf)
            {
                Children = category.Children is not null && category.Children.Count is not 0
                    ? ToResponse(category.Children)
                    : []
            })
            .OrderBy(category => category.Label, StringComparer.OrdinalIgnoreCase);
    }
}
