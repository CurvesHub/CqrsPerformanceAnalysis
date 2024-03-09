using JetBrains.Annotations;

namespace Traditional.Api.UseCases.Categories.GetCategoryMapping;

/// <summary>
/// Represents the response for the get categories endpoint.
/// </summary>
/// <param name="CategoryNumber">The category number of the current category node.</param>
/// <param name="CategoryPath">The category path or name to be displayed to the user.</param>
/// <param name="GermanCategoryNumber">The category number of the german category.</param>
/// <param name="GermanCategoryPath">The german category path or name to be displayed to the user.</param>
[PublicAPI]
public record GetCategoryMappingResponse(
    long? CategoryNumber,
    string? CategoryPath,
    long? GermanCategoryNumber,
    string? GermanCategoryPath);
