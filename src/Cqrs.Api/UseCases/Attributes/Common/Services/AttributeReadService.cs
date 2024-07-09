using Cqrs.Api.Common.BaseRequests;
using Cqrs.Api.UseCases.Articles.Errors;
using Cqrs.Api.UseCases.Articles.Persistence.Repositories;
using Cqrs.Api.UseCases.Attributes.Common.Models;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Repositories;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Repositories;
using ErrorOr;
using Attribute = Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Cqrs.Api.UseCases.Attributes.Common.Services;

/// <summary>
/// Provides attribute related functionality.
/// </summary>
/// <param name="_categoryReadRepository">The category repository.</param>
/// <param name="_articleReadRepository">The article repository.</param>
/// <param name="_attributeReadRepository">The attribute repository.</param>
public class AttributeReadService(
    ICategoryReadRepository _categoryReadRepository,
    IArticleReadRepository _articleReadRepository,
    IAttributeReadRepository _attributeReadRepository)
{
    /// <summary>
    /// Get the article DTOs and the mapped category id for the requested article number.
    /// </summary>
    /// <param name="query">The request.</param>
    /// <returns>A <see cref="ErrorOr.Error"/> or a tuple of the article DTOs and the mapped category id.</returns>
    public async Task<ErrorOr<(List<ArticleDto>, int CategoryId)>> GetArticleDtosAndMappedCategoryIdAsync(BaseQuery query)
    {
        var articleDtos = await _articleReadRepository.GetArticleDtos(query.ArticleNumber).ToListAsync();

        if (articleDtos.Count == 0)
        {
            return ArticleErrors.ArticleNotFound(query.ArticleNumber);
        }

        var mappedCategoryId = await _categoryReadRepository.GetMappedCategoryIdByRootCategoryId(query.ArticleNumber, query.RootCategoryId);

        if (mappedCategoryId is null)
        {
            return ArticleErrors.MappedCategoriesForArticleNotFound(query.ArticleNumber, query.RootCategoryId);
        }

        return (articleDtos, mappedCategoryId.Value);
    }

    /// <summary>
    /// Get the attributes and sub attributes with values for the given article ids.
    /// </summary>
    /// <param name="articleIds">The article ids to search for.</param>
    /// <param name="rootCategoryId">The root category id to search for.</param>
    /// <param name="productTypeIds">The product type ids to search for.</param>
    /// <returns>A list of tuples of the attribute and the attribute value DTOs.</returns>
    public async Task<List<(Attribute Attribute, List<AttributeValueDto> AttributeValueDtos)>> GetAttributesAndSubAttributesWithValuesAsync(
            List<int> articleIds,
            int rootCategoryId,
            List<int> productTypeIds)
    {
        var setProductTypeId = await _attributeReadRepository
            .GetFirstAttributeIdsForTrueProductTypesByArticleIdsAndRootCategoryId(articleIds, rootCategoryId);

        if (setProductTypeId is not null)
        {
            productTypeIds = [.. productTypeIds, setProductTypeId.Value];
        }

        productTypeIds = productTypeIds.Distinct().ToList();

        var attributes = await _attributeReadRepository
            .GetAttributesAndSubAttributesFlatRecursivelyAsNoTracking(productTypeIds)
            .ToListAsync();

        var attributeValueDtos = await _attributeReadRepository
                .LoadAttributeValueDataAsync(attributes.Select(attribute => attribute.Id), articleIds);

        return attributes.ConvertAll(attribute =>
            (attribute, attributeValueDtos.Where(value => value.AttributeId == attribute.Id).ToList()));
    }
}
