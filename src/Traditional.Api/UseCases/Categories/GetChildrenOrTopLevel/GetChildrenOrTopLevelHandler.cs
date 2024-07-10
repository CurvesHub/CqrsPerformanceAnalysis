using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Traditional.Api.Common.DataAccess.Persistence;
using Traditional.Api.UseCases.Categories.Common.Errors;
using Traditional.Api.UseCases.Categories.Common.Persistence.Entities;
using Traditional.Api.UseCases.Categories.Common.Persistence.Repositories;

namespace Traditional.Api.UseCases.Categories.GetChildrenOrTopLevel;

/// <summary>
/// Provides functionality to get child categories or the top level categories based on the request.
/// </summary>
public class GetChildrenOrTopLevelHandler(ICategoryRepository _categoryRepository, TraditionalDbContext _dbContext)
{
    /// <summary>
    /// Gets the child categories or the top level categories (without their children) based on the request.
    /// </summary>
    /// <param name="request">Provides the information for which categories should be retrieved.</param>
    /// <returns>An <see cref="ErrorOr.Error"/> or a list of <see cref="Category"/>s.</returns>
    public async Task<ErrorOr<IEnumerable<Category>>> GetChildrenAsync(GetChildrenOrTopLevelRequest request)
    {
        // 1. Retrieve the requested categories
        List<Category> categories;

        if (request.CategoryNumber is null or 0)
        {
            // If the category number is null or 0, then the first level of the category tree is requested
            categories = await GetTopLevelCategories(request.RootCategoryId).ToListAsync();

            if (categories.Count is 0)
            {
                return CategoryErrors.CategoriesNotFound(request.RootCategoryId);
            }
        }
        else
        {
            // If the parent category is not found return a not found error
            if (!await CategoryExists(request.RootCategoryId, request.CategoryNumber.Value))
            {
                return CategoryErrors.CategoryNotFound(request.CategoryNumber.Value, request.RootCategoryId);
            }

            // If the category number is not null, then the child categories of the parent are requested
            categories = await GetChildren(request.RootCategoryId, request.CategoryNumber.Value).ToListAsync();
        }

        // If no children are found return an empty list
        if (categories.Count is 0)
        {
            return Enumerable.Empty<Category>().ToErrorOr();
        }

        // 2. Check if a category is mapped to the article and set the IsSelected property
        await SetIsSelectedForMappedCategory(
            request.ArticleNumber,
            request.RootCategoryId,
            categories);

        // 3. Return the categories
        return categories;
    }

    private async Task SetIsSelectedForMappedCategory(string articleNumber, int rootCategoryId, List<Category> categories)
    {
        var mappedCategory = await _categoryRepository.GetMappedCategoryByRootCategoryId(articleNumber, rootCategoryId);
        if (mappedCategory is null)
        {
            return;
        }

        var matchingCategory = categories.Find(category => category.CategoryNumber == mappedCategory.CategoryNumber);
        if (matchingCategory is not null)
        {
            // Set the IsSelected property to true for the category that is mapped to the article
            matchingCategory.IsSelected = true;
        }
    }

    /// <summary>
    /// Gets the top level categories based on the root category id.
    /// </summary>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{Category}"/> of <see cref="Category"/>s.</returns>
    private IAsyncEnumerable<Category> GetTopLevelCategories(int rootCategoryId)
    {
        return _dbContext.Categories
            .Where(category => category.RootCategoryId == rootCategoryId && category.ParentId == null)
            .AsAsyncEnumerable();
    }

    /// <summary>
    /// Gets the categories by the category number and the root category id.
    /// </summary>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <param name="categoryNumber">The category number to search for.</param>
    /// <returns>A <see cref="Category"/> or <see langword="null"/> if not found.</returns>
    private async Task<bool> CategoryExists(int rootCategoryId, long categoryNumber)
    {
        return await _dbContext.Categories
            .AnyAsync(category =>
                category.RootCategoryId == rootCategoryId
                && category.CategoryNumber == categoryNumber);
    }

    /// <summary>
    /// Gets the children of a category based on the root category id and the category number.
    /// </summary>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <param name="categoryNumber">The category number to search for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{Category}"/> of <see cref="Category"/>s.</returns>
    private IAsyncEnumerable<Category> GetChildren(int rootCategoryId, long categoryNumber)
    {
        return _dbContext.Categories
            .Where(category => category.RootCategoryId == rootCategoryId && category.Parent!.CategoryNumber == categoryNumber)
            .AsAsyncEnumerable();
    }
}
