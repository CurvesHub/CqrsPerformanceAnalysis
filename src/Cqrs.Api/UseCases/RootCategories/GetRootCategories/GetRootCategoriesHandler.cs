using Cqrs.Api.Common.Interfaces;
using Cqrs.Api.UseCases.RootCategories.Common.Persistence.Entities;

namespace Cqrs.Api.UseCases.RootCategories.GetRootCategories;

/// <summary>
/// Defines the handler for retrieving root categories.
/// </summary>
public class GetRootCategoriesHandler(ICachedRepository<RootCategory> _rootCategoryRepository)
{
    /// <summary>
    /// Gets all root categories.
    /// </summary>
    /// <returns>An enumerable of root categories.</returns>
    public async Task<IEnumerable<RootCategory>> GetRootCategoriesAsync()
    {
        return await _rootCategoryRepository.GetAllAsync();
    }
}
