using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.UseCases.Categories.Common.Errors;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using ErrorOr;
using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cqrs.Api.UseCases.Categories.Queries.GetChildrenOrTopLevel;

/// <summary>
/// Provides functionality to get child categories or the top level categories based on the request.
/// </summary>
[UsedImplicitly]
public class GetChildrenOrTopLevelQueryHandler(CqrsReadDbContext _dbContext) : IRequestHandler<GetChildrenOrTopLevelQuery, ErrorOr<IOrderedEnumerable<GetChildrenOrTopLevelResponse>>>
{
    /// <summary>
    /// Gets the child categories or the top level categories (without their children) based on the request.
    /// </summary>
    /// <param name="query">Provides the information for which categories should be retrieved.</param>
    /// <param name="cancellationToken">The token to cancel the requests.</param>
    /// <returns>An <see cref="ErrorOr.Error"/> or a list of <see cref="Category"/>s.</returns>
    public async Task<ErrorOr<IOrderedEnumerable<GetChildrenOrTopLevelResponse>>> Handle(GetChildrenOrTopLevelQuery query, CancellationToken cancellationToken)
    {
        // 1. Retrieve the requested categories
        List<GetChildrenOrTopLevelResponse> responses;

        if (query.CategoryNumber is null or 0)
        {
            // If the category number is null or 0, then the first level of the category tree is requested
            responses = await GetTopLevelCategories(query.RootCategoryId, query.ArticleNumber).ToListAsync();

            if (responses.Count is 0)
            {
                return CategoryErrors.CategoriesNotFound(query.RootCategoryId);
            }
        }
        else
        {
            // If the parent category is not found return a not found error
            if (!await CategoryExists(query.RootCategoryId, query.CategoryNumber.Value))
            {
                return CategoryErrors.CategoryNotFound(query.CategoryNumber.Value, query.RootCategoryId);
            }

            // If the category number is not null, then the child categories of the parent are requested
            responses = await GetChildren(query.RootCategoryId, query.CategoryNumber.Value, query.ArticleNumber).ToListAsync();
        }

        // 2. Return the categories
        return responses.Count is 0
            ? Enumerable.Empty<GetChildrenOrTopLevelResponse>().Order().ToErrorOr()
            : responses.OrderBy(category => category.Label, StringComparer.OrdinalIgnoreCase).ToErrorOr();
    }

    /// <summary>
    /// Gets the top level categories based on the root category id.
    /// </summary>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <param name="articleNumber">The article number to check the mapping for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{Category}"/> of <see cref="Category"/>s.</returns>
    private IAsyncEnumerable<GetChildrenOrTopLevelResponse> GetTopLevelCategories(int rootCategoryId, string articleNumber)
    {
        return _dbContext.Categories
            .Where(category => category.RootCategoryId == rootCategoryId && category.ParentId == null)
            .Select(category => new GetChildrenOrTopLevelResponse(
                category.CategoryNumber,
                category.Name,
                category.Articles!.Any(a => a.ArticleNumber == articleNumber),
                category.ParentId != null))
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
    /// <param name="articleNumber">The article number to check the mapping for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{Category}"/> of <see cref="Category"/>s.</returns>
    private IAsyncEnumerable<GetChildrenOrTopLevelResponse> GetChildren(int rootCategoryId, long categoryNumber, string articleNumber)
    {
        return _dbContext.Categories
            .Where(category => category.RootCategoryId == rootCategoryId && category.Parent!.CategoryNumber == categoryNumber)
            .Select(category => new GetChildrenOrTopLevelResponse(
                category.CategoryNumber,
                category.Name,
                category.Articles!.Any(a => a.ArticleNumber == articleNumber),
                category.ParentId != null))
            .AsAsyncEnumerable();
    }
}
