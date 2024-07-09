using System.Diagnostics.CodeAnalysis;
using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.UseCases.Articles.Persistence.Entities;
using Cqrs.Api.UseCases.Attributes.Common.Models;
using Microsoft.EntityFrameworkCore;

namespace Cqrs.Api.UseCases.Articles.Persistence.Repositories;

/// <inheritdoc />
[SuppressMessage("Performance", "MA0020:Use direct methods instead of LINQ methods", Justification = "Not possible with EF Core linq queries")]
internal class ArticleReadRepository(CqrsReadDbContext _dbContext) : IArticleReadRepository
{
    /// <inheritdoc />
    public Task<Article?> GetFirstByNumberWithCategories(string articleNumber)
    {
        return _dbContext.Articles
            .Include(article => article.Categories)!
            .ThenInclude(category => category.RootCategory)
            .FirstOrDefaultAsync(article => article.ArticleNumber == articleNumber);
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
}
