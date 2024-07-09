using Cqrs.Api.Common.BaseRequests;
using Cqrs.Api.UseCases.Articles.Errors;
using Cqrs.Api.UseCases.Articles.Persistence.Repositories;
using Cqrs.Api.UseCases.RootCategories.Common.Persistence.Entities;
using ErrorOr;

namespace Cqrs.Api.UseCases.Categories.Queries.GetCategoryMapping;

/// <summary>
/// Handler for querying category mapping for articles.
/// </summary>
public class GetCategoryMappingQueryHandler(IArticleReadRepository _articleReadRepository)
{
    /// <summary>
    /// Gets the associated category for the article based on the request.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>An error or an <see cref="GetCategoryMappingResponse"/>.</returns>
    public async Task<ErrorOr<GetCategoryMappingResponse>> GetCategoryMappingAsync(BaseRequest request)
    {
        // 1. Get the first article (all variants are in the same category) with the categories (one category per RootCategory possible)
        var article = await _articleReadRepository.GetFirstByNumberWithCategories(request.ArticleNumber);

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
