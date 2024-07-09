using System.Diagnostics.CodeAnalysis;
using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.UseCases.Articles.Persistence.Entities;
using Cqrs.Api.UseCases.Attributes.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Cqrs.Api.UseCases.Articles.Persistence.Repositories;

/// <inheritdoc />
[SuppressMessage("Performance", "MA0020:Use direct methods instead of LINQ methods", Justification = "Not possible with EF Core linq queries")]
internal class ArticleWriteRepository(CqrsWriteDbContext _dbContext) : IArticleWriteRepository
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
