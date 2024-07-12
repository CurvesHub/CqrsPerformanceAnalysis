using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.UseCases.Articles.Errors;
using Cqrs.Api.UseCases.RootCategories.Common.Persistence.Entities;
using ErrorOr;
using JetBrains.Annotations;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cqrs.Api.UseCases.Categories.Queries.GetCategoryMapping;

/// <summary>
/// Handler for querying category mapping for articles.
/// </summary>
[UsedImplicitly]
public class GetCategoryMappingQueryHandler(CqrsReadDbContext _dbContext) : IRequestHandler<GetCategoryMappingQuery, ErrorOr<GetCategoryMappingResponse>>
{
    /// <summary>
    /// Gets the associated category for the article based on the request.
    /// </summary>
    /// <param name="query">The request.</param>
    /// <param name="cancellationToken">The token to cancel the requests.</param>
    /// <returns>An error or an <see cref="GetCategoryMappingResponse"/>.</returns>
    public async Task<ErrorOr<GetCategoryMappingResponse>> Handle(GetCategoryMappingQuery query, CancellationToken cancellationToken)
    {
        // 1. Get the mapped category for the article (all variants are in the same category, one category per RootCategory possible)
        var categoryResponses = await _dbContext.Categories
            .Where(category =>
                category.Articles!.Any(article => article.ArticleNumber == query.ArticleNumber)
                && (category.RootCategoryId == query.RootCategoryId
                    || category.RootCategory!.LocaleCode == LocaleCode.de_DE))
            .Select(category => new GetCategoryMappingResponse(
                category.RootCategoryId == query.RootCategoryId ? category.CategoryNumber : null,
                category.RootCategoryId == query.RootCategoryId ? category.Path : null,
                category.RootCategory!.LocaleCode == LocaleCode.de_DE ? category.CategoryNumber : null,
                category.RootCategory!.LocaleCode == LocaleCode.de_DE ? category.Path : null))
            .ToListAsync();

        // 2. Merge the category responses
        var response = new GetCategoryMappingResponse(
            categoryResponses.Find(response => response.CategoryNumber is not null)?.CategoryNumber,
            categoryResponses.Find(response => response.CategoryPath is not null)?.CategoryPath,
            categoryResponses.Find(response => response.GermanCategoryNumber is not null)?.GermanCategoryNumber,
            categoryResponses.Find(response => response.GermanCategoryPath is not null)?.GermanCategoryPath);

        if (categoryResponses.Count is 0 ||
            (response.CategoryNumber is null
            && response.CategoryPath is null
            && response.GermanCategoryNumber is null
            && response.GermanCategoryPath is null))
        {
            return ArticleErrors.MappedCategoriesForArticleNotFound(query.ArticleNumber, query.RootCategoryId);
        }

        // 3. Return the mapped category
        return response;
    }
}
