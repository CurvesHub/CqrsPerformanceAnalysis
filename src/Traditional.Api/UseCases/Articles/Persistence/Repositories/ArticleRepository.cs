using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Traditional.Api.Common.DataAccess.Persistence;
using Traditional.Api.UseCases.Articles.Persistence.Entities;
using Traditional.Api.UseCases.Attributes.Common.Models;

namespace Traditional.Api.UseCases.Articles.Persistence.Repositories;

/// <inheritdoc />
[SuppressMessage("Performance", "MA0020:Use direct methods instead of LINQ methods", Justification = "Not possible with EF Core linq queries")]
internal class ArticleRepository(TraditionalDbContext _dbContext) : IArticleRepository
{
    /// <inheritdoc />
    public IAsyncEnumerable<Article> GetByNumberWithCategoriesByRootCategoryId(string articleNumber, int rootCategoryId)
    {
        return _dbContext.Articles
            .Where(article => article.ArticleNumber == articleNumber)
            .Include(article => article.Categories!
                .Where(category => category.RootCategoryId == rootCategoryId))
            .AsAsyncEnumerable();
    }

    /// <inheritdoc />
    public Task<Article?> GetFirstByNumberWithCategories(string articleNumber)
    {
        return _dbContext.Articles
            .Include(article => article.Categories)!
            .ThenInclude(category => category.RootCategory)
            .FirstOrDefaultAsync(article => article.ArticleNumber == articleNumber);
    }

    /// <inheritdoc/>
    public async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        _dbContext.ChangeTracker.DetectChanges();
        return await _dbContext.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<ArticleDto> GetArticleDtos(string articleNumber)
    {
        return _dbContext.Articles
            .Where(a => a.ArticleNumber == articleNumber)
            .Select(article => new ArticleDto(article.Id, article.CharacteristicId))
            .ToAsyncEnumerable();
    }

    /// <inheritdoc/>
    public async Task<bool> HasArticleVariantsAsync(string articleNumber)
    {
        return await _dbContext.Articles.AnyAsync(article => article.ArticleNumber == articleNumber && article.CharacteristicId > 0);
    }

    /// <inheritdoc/>
    public IAsyncEnumerable<Article> GetByNumberWithAttributeValuesByRootCategoryId(string articleNumber, int rootCategoryId)
    {
        return _dbContext.Articles
            .AsSplitQuery()
            .Where(a => a.ArticleNumber == articleNumber)
            .Include(article => article.AttributeBooleanValues!.Where(value => value.Attribute!.RootCategoryId == rootCategoryId))
            .Include(article => article.AttributeDecimalValues!.Where(value => value.Attribute!.RootCategoryId == rootCategoryId))
            .Include(article => article.AttributeIntValues!.Where(value => value.Attribute!.RootCategoryId == rootCategoryId))
            .Include(article => article.AttributeStringValues!.Where(value => value.Attribute!.RootCategoryId == rootCategoryId))
            .ToAsyncEnumerable();
    }
}
