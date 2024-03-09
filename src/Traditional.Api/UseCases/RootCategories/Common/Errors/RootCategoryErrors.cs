using System.Globalization;
using ErrorOr;

namespace Traditional.Api.UseCases.RootCategories.Common.Errors;

/// <summary>
/// Defines the root category errors.
/// </summary>
public static class RootCategoryErrors
{
    /// <summary>
    /// Represents the error when the root category id could not be found.
    /// </summary>
    /// <param name="rootCategoryId">The root category id which could not be found.</param>
    /// <returns>A not found <see cref="ErrorOr.Error"/>.</returns>
    public static Error RootCategoryIdNotFound(int rootCategoryId)
        => Error.NotFound(
            code: "RootCategoryIdNotFound",
            description: $"Root category id '{rootCategoryId.ToString(CultureInfo.InvariantCulture)}' could not be found.");
}
