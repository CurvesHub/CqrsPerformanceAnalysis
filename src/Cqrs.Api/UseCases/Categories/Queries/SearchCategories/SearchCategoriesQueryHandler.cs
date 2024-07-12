using System.Linq.Expressions;
using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.Common.Extensions;
using Cqrs.Api.UseCases.Categories.Common.Errors;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using ErrorOr;
using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cqrs.Api.UseCases.Categories.Queries.SearchCategories;

/// <summary>
/// Provides functionality to search for categories.
/// </summary>
[UsedImplicitly]
public class SearchCategoriesQueryHandler(CqrsReadDbContext _dbContext) : IRequestHandler<SearchCategoriesQuery, ErrorOr<IEnumerable<SearchCategoryDto>>>
{
    /// <summary>
    /// Searches for categories based on the request.
    /// </summary>
    /// <param name="query">Provides the information for the search.</param>
    /// <param name="cancellationToken">The token to cancel the requests.</param>
    /// <returns>An <see cref="ErrorOr.Error"/> or a list of <see cref="Category"/>s with all their children.</returns>
    public async Task<ErrorOr<IEnumerable<SearchCategoryDto>>> Handle(SearchCategoriesQuery query, CancellationToken cancellationToken)
    {
        // 1. Retrieve the category and all its parents up to the top level category
        var allCategories = IsSearchTermRequested(query)
            ? await SearchParentsRecursiveBySearchTerm(query.RootCategoryId, query.SearchTerm!, query.ArticleNumber).ToListAsync()
            : await SearchParentsRecursiveByCategoryNumber(query.RootCategoryId, query.CategoryNumber!.Value, query.ArticleNumber).ToListAsync();

        if (allCategories.Count == 0)
        {
            return CategoryErrors.NoResultsForCategorySearch(query);
        }

        // 2. Add each child to each parent category
        if (IsSearchTermRequested(query))
        {
            // For every by the search term requested category: add each child to each parent
            foreach (var category in allCategories.Where(category => category.Label.Contains(query.SearchTerm!, StringComparison.InvariantCultureIgnoreCase)))
            {
                AddEachChildToEachParent(allCategories, category.CategoryNumber);
            }
        }
        else
        {
            // For the one requested category: add each child to each parent
            AddEachChildToEachParent(allCategories, query.CategoryNumber!.Value);
        }

        // 4. Each category has now its requested children, so we can return the top level categories
        return allCategories
            .Where(c => c.ParentCategoryNumber is null)
            .ToErrorOr();
    }

    private static bool IsSearchTermRequested(SearchCategoriesQuery query)
    {
        if (!string.IsNullOrWhiteSpace(query.SearchTerm))
        {
            return true;
        }

        if (query.CategoryNumber is not null and not 0)
        {
            return false;
        }

        // Should never happen since the validation should prevent it
        throw new InvalidOperationException("Either the search term or the category number must be provided.");
    }

    private static void AddEachChildToEachParent(
        IReadOnlyCollection<SearchCategoryDto> allCategories,
        long categoryNumber)
    {
        var leafCategory = allCategories.Single(category => category.CategoryNumber == categoryNumber);

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
            var alreadyExistingChild = parentCategory.Children.Find(child => child.CategoryNumber == currentRoot.CategoryNumber);

            if (alreadyExistingChild is null)
            {
                // Add the current root as child to the parent category
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
    /// <param name="articleNumber">The article number to search for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{Category}"/> of <see cref="Category"/>s.</returns>
    private IAsyncEnumerable<SearchCategoryDto> SearchParentsRecursiveByCategoryNumber(int rootCategoryId, long categoryNumber, string articleNumber)
    {
        return SearchParentsRecursive(
            initialFilter: category =>
                category.RootCategoryId == rootCategoryId
                && category.CategoryNumber == categoryNumber,
            articleNumber: articleNumber);
    }

    /// <summary>
    /// Searches for the parents of a category recursively by the search term.
    /// </summary>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <param name="searchTerm">The search term to search for.</param>
    /// <param name="articleNumber">The article number to search for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{Category}"/> of <see cref="Category"/>s.</returns>
    private IAsyncEnumerable<SearchCategoryDto> SearchParentsRecursiveBySearchTerm(int rootCategoryId, string searchTerm, string articleNumber)
    {
#pragma warning disable RCS1155, MA0011, CA1862
        return SearchParentsRecursive(
            initialFilter: category =>
                category.RootCategoryId == rootCategoryId
                // We cant use the culture invariant here because entity framework core does not support it
                && category.Name.ToLower().Contains(searchTerm.ToLower()),
            articleNumber: articleNumber);
#pragma warning restore CA1862, MA0011, RCS1155
    }

    private IAsyncEnumerable<SearchCategoryDto> SearchParentsRecursive(Expression<Func<Category, bool>> initialFilter, string articleNumber)
    {
        // Hint: Implement integration tests or benchmarks to evaluate the performance of recursive queries
        return _dbContext.Categories.RecursiveCteQuery(
                initialFilter: initialFilter,
                navigationProperty: category => category.Parent)
            .Include(category => category.Parent)
            .Select(category => new SearchCategoryDto(
                category.CategoryNumber,
                category.Name,
                category.Articles!.Any(a => a.ArticleNumber == articleNumber),
                category.ParentId != null,
                category.ParentCategoryNumber))
            .AsAsyncEnumerable();
    }
}
