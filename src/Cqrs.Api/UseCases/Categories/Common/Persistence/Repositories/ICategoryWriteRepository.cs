using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;

namespace Cqrs.Api.UseCases.Categories.Common.Persistence.Repositories;

/// <summary>
/// Handles the data access for the categories.
/// </summary>
public interface ICategoryWriteRepository
{
    /// <summary>
    /// Gets the categories by the category number and the root category id.
    /// </summary>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <param name="categoryNumber">The category number to search for.</param>
    /// <returns>A <see cref="Category"/> or <see langword="null"/> if not found.</returns>
    Task<Category?> GetByNumberAndRootCategoryId(int rootCategoryId, long categoryNumber);

    /// <summary>
    /// Searches for the parents of a category recursively by the category number.
    /// </summary>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <param name="categoryNumber">The category number to search for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{Category}"/> of <see cref="Category"/>s.</returns>
    IAsyncEnumerable<Category> SearchParentsRecursiveByCategoryNumber(int rootCategoryId, long categoryNumber);

    /// <summary>
    /// Searches for the parents of a category recursively by the search term.
    /// </summary>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <param name="searchTerm">The search term to search for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{Category}"/> of <see cref="Category"/>s.</returns>
    IAsyncEnumerable<Category> SearchParentsRecursiveBySearchTerm(int rootCategoryId, string searchTerm);

    /// <summary>
    /// Gets the mapped category by the root category id.
    /// </summary>
    /// <param name="articleNumber">The article number to search for.</param>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <returns>A <see cref="Category"/> or <see langword="null"/>.</returns>
    Task<Category?> GetMappedCategoryByRootCategoryId(string articleNumber, int rootCategoryId);

    /// <summary>
    /// Gets the mapped category id by the root category id.
    /// </summary>
    /// <param name="articleNumber">The article number to search for.</param>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <returns>A <see cref="Category"/> or <see langword="null"/>.</returns>
    Task<int?> GetMappedCategoryIdByRootCategoryId(string articleNumber, int rootCategoryId);

    /// <summary>
    /// Gets the top level categories based on the root category id.
    /// </summary>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{Category}"/> of <see cref="Category"/>s.</returns>
    IAsyncEnumerable<Category> GetTopLevelCategories(int rootCategoryId);

    /// <summary>
    /// Gets the children of a category based on the root category id and the category number.
    /// </summary>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <param name="categoryNumber">The category number to search for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{Category}"/> of <see cref="Category"/>s.</returns>
    IAsyncEnumerable<Category> GetChildren(int rootCategoryId, long categoryNumber);
}
