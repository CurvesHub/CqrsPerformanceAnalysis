using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;

namespace Cqrs.Api.UseCases.Categories.Common.Persistence.Repositories;

/// <summary>
/// Handles the data access for the categories.
/// </summary>
public interface ICategoryReadRepository
{
    /// <summary>
    /// Gets the mapped category id by the root category id.
    /// </summary>
    /// <param name="articleNumber">The article number to search for.</param>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <returns>A <see cref="Category"/> or <see langword="null"/>.</returns>
    Task<int?> GetMappedCategoryIdByRootCategoryId(string articleNumber, int rootCategoryId);
}
