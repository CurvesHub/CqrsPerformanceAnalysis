using System.Diagnostics.CodeAnalysis;
using System.Linq.Expressions;
using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.Common.Services;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using Microsoft.EntityFrameworkCore;

namespace Cqrs.Api.UseCases.Categories.Common.Persistence.Repositories;

/// <inheritdoc />
[SuppressMessage("Performance", "MA0020:Use direct methods instead of LINQ methods", Justification = "Not possible with EF Core linq queries")]
internal class CategoryReadRepository(CqrsReadDbContext _dbContext) : ICategoryReadRepository
{
    /// <inheritdoc />
    public async Task<Category?> GetByNumberAndRootCategoryId(int rootCategoryId, long categoryNumber)
    {
        return await _dbContext.Categories
            .SingleOrDefaultAsync(category =>
                category.RootCategoryId == rootCategoryId
                && category.CategoryNumber == categoryNumber);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<Category> SearchParentsRecursiveByCategoryNumber(int rootCategoryId, long categoryNumber)
    {
        return SearchParentsRecursive(category =>
            category.RootCategoryId == rootCategoryId
            && category.CategoryNumber == categoryNumber);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<Category> SearchParentsRecursiveBySearchTerm(int rootCategoryId, string searchTerm)
    {
        return SearchParentsRecursive(category =>
            category.RootCategoryId == rootCategoryId
#pragma warning disable RCS1155, MA0011, CA1862
            // We cant use the culture invariant here because entity framework core does not support it
            && category.Name.ToLower().Contains(searchTerm.ToLower()));
#pragma warning restore CA1862, MA0011, RCS1155
    }

    /// <inheritdoc />
    public async Task<Category?> GetMappedCategoryByRootCategoryId(string articleNumber, int rootCategoryId)
    {
        return await _dbContext.Categories
            .SingleOrDefaultAsync(category =>
                category.RootCategoryId == rootCategoryId
                && category.Articles!.Any(a => a.ArticleNumber == articleNumber));
    }

    /// <inheritdoc />
    public async Task<int?> GetMappedCategoryIdByRootCategoryId(string articleNumber, int rootCategoryId)
    {
        return await _dbContext.Categories
            .Where(category => category.RootCategoryId == rootCategoryId && category.Articles!.Any(article => article.ArticleNumber == articleNumber))
            .Select(category => (int?)category.Id)
            .SingleOrDefaultAsync();
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<Category> GetTopLevelCategories(int rootCategoryId)
    {
        return _dbContext.Categories
            .Where(category => category.RootCategoryId == rootCategoryId && category.ParentId == null)
            .AsAsyncEnumerable();
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<Category> GetChildren(int rootCategoryId, long categoryNumber)
    {
        return _dbContext.Categories
            .Where(category => category.RootCategoryId == rootCategoryId && category.Parent!.CategoryNumber == categoryNumber)
            .AsAsyncEnumerable();
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
