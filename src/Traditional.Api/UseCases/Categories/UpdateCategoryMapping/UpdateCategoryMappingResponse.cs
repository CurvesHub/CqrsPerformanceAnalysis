using JetBrains.Annotations;

namespace Traditional.Api.UseCases.Categories.UpdateCategoryMapping;

/// <summary>
/// Represents the response for updating the category mappings of an article.
/// </summary>
/// <param name="CategoryNumber">The new category number of the categories associated with the article.</param>
/// <param name="CategoryPath">The new category path of the categories associated with the article.</param>
[PublicAPI]
public record UpdateCategoryMappingResponse(
    long CategoryNumber,
    string CategoryPath);
