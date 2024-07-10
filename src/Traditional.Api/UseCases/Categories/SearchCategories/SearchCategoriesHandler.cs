using System.Linq.Expressions;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Traditional.Api.Common.DataAccess.Persistence;
using Traditional.Api.Common.Extensions;
using Traditional.Api.UseCases.Categories.Common.Errors;
using Traditional.Api.UseCases.Categories.Common.Persistence.Entities;
using Traditional.Api.UseCases.Categories.Common.Persistence.Repositories;

namespace Traditional.Api.UseCases.Categories.SearchCategories;

/// <summary>
/// Provides functionality to search for categories.
/// </summary>
public class SearchCategoriesHandler(TraditionalDbContext _dbContext, ICategoryRepository _categoryRepository)
{
    /// <summary>
    /// Searches for categories based on the request.
    /// </summary>
    /// <param name="request">Provides the information for the search.</param>
    /// <returns>An <see cref="ErrorOr.Error"/> or a list of <see cref="Category"/>s with all their children.</returns>
    public async Task<ErrorOr<IEnumerable<Category>>> SearchCategoriesAsync(
        SearchCategoriesRequest request)
    {
        // 1. Retrieve the category and all its parents up to the top level category
        var allCategories = IsSearchTermRequested(request)
            ? await SearchParentsRecursiveBySearchTerm(request.RootCategoryId, request.SearchTerm!).ToListAsync()
            : await SearchParentsRecursiveByCategoryNumber(request.RootCategoryId, request.CategoryNumber!.Value).ToListAsync();

        if (allCategories.Count == 0)
        {
            return CategoryErrors.NoResultsForCategorySearch(request);
        }

        // 2. Retrieve the mapped category for the article
        var mappedCategory = await _categoryRepository.GetMappedCategoryByRootCategoryId(
            request.ArticleNumber,
            request.RootCategoryId);

        // 3. Add each child to each parent category
        if (IsSearchTermRequested(request))
        {
            // For every by the search term requested category: add each child to each parent
            foreach (var category in allCategories.Where(category => category.Name.Contains(request.SearchTerm!, StringComparison.InvariantCultureIgnoreCase)))
            {
                AddEachChildToEachParent(allCategories, category.CategoryNumber, mappedCategory?.CategoryNumber);
            }
        }
        else
        {
            // For the one requested category: add each child to each parent
            AddEachChildToEachParent(allCategories, request.CategoryNumber!.Value, mappedCategory?.CategoryNumber);
        }

        // 4. Each category has now its requested children, so we can return the top level categories
        return allCategories
            .Where(c => c.ParentCategoryNumber is null)
            .ToErrorOr();
    }

    private static bool IsSearchTermRequested(SearchCategoriesRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.SearchTerm))
        {
            return true;
        }

        if (request.CategoryNumber is not null and not 0)
        {
            return false;
        }

        // Should never happen since the validation should prevent this
        throw new InvalidOperationException("Either the search term or the category number must be provided.");
    }

    private static void AddEachChildToEachParent(
        IReadOnlyCollection<Category> allCategories,
        long categoryNumber,
        long? mappedCategoryNumber)
    {
        var leafCategory = allCategories.Single(category => category.CategoryNumber == categoryNumber);

        // Set the is selected property to indicate that the category is the mapped category
        leafCategory.IsSelected = mappedCategoryNumber is not null && leafCategory.CategoryNumber == mappedCategoryNumber;

        // Add each child to its parent category
        var currentRoot = leafCategory;
        while (true)
        {
            // If the current root has no further parent return it
            if (currentRoot.ParentCategoryNumber is null or 0)
            {
                return;
            }

            var parentCategory = allCategories.Single(category => category.CategoryNumber == currentRoot.ParentCategoryNumber);

            // Check if the currentRoot is already in the parent category children.
            var alreadyExistingChild = parentCategory.Children?.Find(child => child.CategoryNumber == currentRoot.CategoryNumber);

            if (alreadyExistingChild is null)
            {
                // Add the current root as child to the parent category
                parentCategory.Children ??= [];
                parentCategory.Children.Add(currentRoot);
            }

            // Set the parent category as the new current root
            currentRoot = parentCategory;
        }
    }

    /// <summary>
    /// Searches for the parents of a category recursively by the category number.
    /// </summary>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <param name="categoryNumber">The category number to search for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{Category}"/> of <see cref="Category"/>s.</returns>
    private IAsyncEnumerable<Category> SearchParentsRecursiveByCategoryNumber(int rootCategoryId, long categoryNumber)
    {
        return SearchParentsRecursive(category =>
            category.RootCategoryId == rootCategoryId
            && category.CategoryNumber == categoryNumber);
    }

    /// <summary>
    /// Searches for the parents of a category recursively by the search term.
    /// </summary>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <param name="searchTerm">The search term to search for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{Category}"/> of <see cref="Category"/>s.</returns>
    private IAsyncEnumerable<Category> SearchParentsRecursiveBySearchTerm(int rootCategoryId, string searchTerm)
    {
        return SearchParentsRecursive(category =>
            category.RootCategoryId == rootCategoryId
#pragma warning disable RCS1155, MA0011, CA1862
            // We cant use the culture invariant here because entity framework core does not support it
            && category.Name.ToLower().Contains(searchTerm.ToLower()));
#pragma warning restore CA1862, MA0011, RCS1155
    }

    private IAsyncEnumerable<Category> SearchParentsRecursive(Expression<Func<Category, bool>> initialFilter)
    {
        // Hint: Implement integration tests or benchmarks to evaluate the performance of recursive queries
        return _dbContext.Categories.RecursiveCteQuery(
                initialFilter: initialFilter,
                navigationProperty: category => category.Parent)
            .Include(category => category.Parent)
            .AsNoTracking()
            .AsAsyncEnumerable();
    }
}
