using System.Net;
using Cqrs.Api.Common.Constants;
using Cqrs.Api.Common.Endpoints;
using Cqrs.Api.Common.ErrorHandling;
using Microsoft.AspNetCore.Mvc;

namespace Cqrs.Api.UseCases.Categories.Queries.SearchCategories;

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
            .AddEndpointFilter<ValidationFilter<SearchCategoriesQuery>>()
            .WithOpenApi();
    }

    private static async Task<IResult> SearchCategoriesAsync(
        [AsParameters] SearchCategoriesQuery query,
        [FromServices] SearchCategoriesQueryHandler queryHandler,
        [FromServices] HttpProblemDetailsService problemDetailsService)
    {
        var result = await queryHandler.SearchCategoriesAsync(query);

        return result.Match(
            categories => Results.Ok(ToResponse(categories)),
            problemDetailsService.LogErrorsAndReturnProblem);
    }

    private static IOrderedEnumerable<SearchCategoriesResponse> ToResponse(
        IEnumerable<SearchCategoryDto> categories)
    {
        return categories.Select(category =>
            new SearchCategoriesResponse(
                category.CategoryNumber,
                category.Label,
                category.IsSelected,
                category.IsLeaf)
            {
                Children = category.Children.Count is not 0
                    ? ToResponse(category.Children)
                    : []
            })
            .OrderBy(category => category.Label, StringComparer.OrdinalIgnoreCase);
    }
}
