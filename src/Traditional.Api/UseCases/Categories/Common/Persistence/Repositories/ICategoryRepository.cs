using Traditional.Api.UseCases.Categories.Common.Persistence.Entities;

namespace Traditional.Api.UseCases.Categories.Common.Persistence.Repositories;

/// <summary>
/// Handles the data access for the categories.
/// </summary>
public interface ICategoryRepository
{
    /// <summary>
    /// Gets the mapped category by the root category id.
    /// </summary>
    /// <param name="articleNumber">The article number to search for.</param>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <returns>A <see cref="Category"/> or <see langword="null"/>.</returns>
    Task<Category?> GetMappedCategoryByRootCategoryId(string articleNumber, int rootCategoryId);
}
