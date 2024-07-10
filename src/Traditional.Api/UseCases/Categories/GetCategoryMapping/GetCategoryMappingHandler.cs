using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Traditional.Api.Common.BaseRequests;
using Traditional.Api.Common.DataAccess.Persistence;
using Traditional.Api.UseCases.Articles.Errors;
using Traditional.Api.UseCases.RootCategories.Common.Persistence.Entities;

namespace Traditional.Api.UseCases.Categories.GetCategoryMapping;

/// <summary>
/// Handler for querying category mapping for articles.
/// </summary>
public class GetCategoryMappingHandler(TraditionalDbContext _dbContext)
{
    /// <summary>
    /// Gets the associated category for the article based on the request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>An error or an <see cref="GetCategoryMappingResponse"/>.</returns>
    public async Task<ErrorOr<GetCategoryMappingResponse>> GetCategoryMappingAsync(BaseRequest request)
    {
        // 1. Get the first article (all variants are in the same category) with the categories (one category per RootCategory possible)
        var article = await _dbContext.Articles
            .Include(a => a.Categories)!
            .ThenInclude(category => category.RootCategory)
            .FirstOrDefaultAsync(a => a.ArticleNumber == request.ArticleNumber);

        if (article is null)
        {
            return ArticleErrors.ArticleNotFound(request.ArticleNumber);
        }

        // 2. Get German mapped category for the default path and number if it exists
        var germanMappedCategory = article.Categories?.SingleOrDefault(category =>
            category.RootCategory!.LocaleCode == LocaleCode.de_DE);

        // 3. Get the requested mapped category
        var requestedMappedCategory = article.Categories?.SingleOrDefault(x => x.RootCategoryId == request.RootCategoryId);

        if (requestedMappedCategory is null && germanMappedCategory is null)
        {
            return ArticleErrors.MappedCategoriesForArticleNotFound(request.ArticleNumber, request.RootCategoryId);
        }

        return new GetCategoryMappingResponse(
            CategoryNumber: requestedMappedCategory?.CategoryNumber,
            CategoryPath: requestedMappedCategory?.Path,
            GermanCategoryNumber: germanMappedCategory?.CategoryNumber,
            GermanCategoryPath: germanMappedCategory?.Path);
    }
}
