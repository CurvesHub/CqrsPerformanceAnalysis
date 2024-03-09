using ErrorOr;
using Traditional.Api.Common.BaseRequests;
using Traditional.Api.Common.Interfaces;
using Traditional.Api.UseCases.Articles.Errors;
using Traditional.Api.UseCases.Attributes.Common.Models;
using Attribute = Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Traditional.Api.UseCases.Attributes.Common.Services;

/// <summary>
/// Provides attribute related functionality.
/// </summary>
/// <param name="_categoryRepository">The category repository.</param>
/// <param name="_articleRepository">The article repository.</param>
/// <param name="_attributeRepository">The attribute repository.</param>
public class AttributeService(
    ICategoryRepository _categoryRepository,
    IArticleRepository _articleRepository,
    IAttributeRepository _attributeRepository)
{
    /// <summary>
    /// Get the article DTOs and the mapped category id for the requested article number.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>A <see cref="ErrorOr.Error"/> or a tuple of the article DTOs and the mapped category id.</returns>
    public async Task<ErrorOr<(List<ArticleDto>, int CategoryId)>> GetArticleDtosAndMappedCategoryIdAsync(BaseRequest request)
    {
        var articleDtos = await _articleRepository.GetArticleDtos(request.ArticleNumber).ToListAsync();

        if (articleDtos.Count == 0)
        {
            return ArticleErrors.ArticleNotFound(request.ArticleNumber);
        }

        var mappedCategoryId = await _categoryRepository.GetMappedCategoryIdByRootCategoryId(request.ArticleNumber, request.RootCategoryId);

        if (mappedCategoryId is null)
        {
            return ArticleErrors.MappedCategoriesForArticleNotFound(request.ArticleNumber, request.RootCategoryId);
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
        var setProductTypeId = await _attributeRepository
            .GetFirstAttributeIdsForTrueProductTypesByArticleIdsAndRootCategoryId(articleIds, rootCategoryId);

        if (setProductTypeId is not null)
        {
            productTypeIds = [.. productTypeIds, setProductTypeId.Value];
        }

        productTypeIds = productTypeIds.Distinct().ToList();

        var attributes = await _attributeRepository
            .GetAttributesAndSubAttributesFlatRecursivelyAsNoTracking(productTypeIds)
            .ToListAsync();

        var attributeValueDtos = await _attributeRepository
                .LoadAttributeValueDataAsync(attributes.Select(attribute => attribute.Id), articleIds);

        return attributes.ConvertAll(attribute =>
            (attribute, attributeValueDtos.Where(value => value.AttributeId == attribute.Id).ToList()));
    }
}
