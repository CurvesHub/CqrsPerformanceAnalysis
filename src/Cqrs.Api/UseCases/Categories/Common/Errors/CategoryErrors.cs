using System.Globalization;
using Cqrs.Api.UseCases.Categories.SearchCategories;
using ErrorOr;

namespace Cqrs.Api.UseCases.Categories.Common.Errors;

/// <summary>
/// Defines the category errors.
/// </summary>
public static class CategoryErrors
{
    /// <summary>
    /// Produces an error when the category could not be found.
    /// </summary>
    /// <param name="categoryNumber">The category number which could not be found.</param>
    /// <param name="rootCategoryId">The root category id which could not be found.</param>
    /// <returns>A not found <see cref="ErrorOr.Error"/>.</returns>
    public static Error CategoryNotFound(long categoryNumber, int rootCategoryId)
        => Error.NotFound(
            code: "CategoryNotfound",
            description: $"Category with number '{categoryNumber.ToString(CultureInfo.InvariantCulture)}' and root category id '{rootCategoryId.ToString(CultureInfo.InvariantCulture)}' could not be found.");

    /// <summary>
    /// Produces an error when the categories could not be found.
    /// </summary>
    /// <param name="rootCategoryId">The root category id which could not be found.</param>
    /// <returns>A not found <see cref="ErrorOr.Error"/>.</returns>
    public static Error CategoriesNotFound(int rootCategoryId)
        => Error.NotFound(
            code: "CategoriesNotfound",
            description: $"Could not find any categories for root category id '{rootCategoryId.ToString(CultureInfo.InvariantCulture)}'");

    /// <summary>
    /// Produces an error when the category search did not return any results.
    /// </summary>
    /// <param name="request">The request which did not return any results.</param>
    /// <returns>A not found <see cref="ErrorOr.Error"/>.</returns>
    public static Error NoResultsForCategorySearch(SearchCategoriesRequest request)
        => Error.NotFound(
            code: "NoResultsForCategorySearch",
            description: "The category search did not return any results for the request.",
            metadata: new Dictionary<string, object>(StringComparer.Ordinal) { { "request", request } });
}
