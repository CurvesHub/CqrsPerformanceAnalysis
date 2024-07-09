using Cqrs.Api.UseCases.Categories.Common.Errors;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Repositories;
using ErrorOr;

namespace Cqrs.Api.UseCases.Categories.Queries.GetChildrenOrTopLevel;

/// <summary>
/// Provides functionality to get child categories or the top level categories based on the request.
/// </summary>
public class GetChildrenOrTopLevelHandler(ICategoryWriteRepository _categoryWriteRepository)
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
            categories = await _categoryWriteRepository.GetTopLevelCategories(request.RootCategoryId).ToListAsync();

            if (categories.Count is 0)
            {
                return CategoryErrors.CategoriesNotFound(request.RootCategoryId);
            }
        }
        else
        {
            var parentCategory = await _categoryWriteRepository.GetByNumberAndRootCategoryId(request.RootCategoryId, request.CategoryNumber.Value);

            // If the parent category is not found return a not found error
            if (parentCategory is null)
            {
                return CategoryErrors.CategoryNotFound(request.CategoryNumber.Value, request.RootCategoryId);
            }

            // If the category number is not null, then the child categories of the parent are requested
            categories = await _categoryWriteRepository.GetChildren(request.RootCategoryId, request.CategoryNumber.Value).ToListAsync();
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
        var mappedCategory = await _categoryWriteRepository.GetMappedCategoryByRootCategoryId(articleNumber, rootCategoryId);
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
}
