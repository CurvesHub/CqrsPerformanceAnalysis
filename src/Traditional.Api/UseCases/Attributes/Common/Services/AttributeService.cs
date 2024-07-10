using System.Globalization;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Traditional.Api.Common.BaseRequests;
using Traditional.Api.Common.DataAccess.Persistence;
using Traditional.Api.Common.Extensions;
using Traditional.Api.UseCases.Articles.Errors;
using Traditional.Api.UseCases.Attributes.Common.Models;
using Attribute = Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Traditional.Api.UseCases.Attributes.Common.Services;

/// <summary>
/// Provides attribute related functionality.
/// </summary>
/// <param name="_dbContext">The database context.</param>
public class AttributeService(TraditionalDbContext _dbContext)
{
    /// <summary>
    /// Get the article DTOs and the mapped category id for the requested article number.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>A <see cref="ErrorOr.Error"/> or a tuple of the article DTOs and the mapped category id.</returns>
    public async Task<ErrorOr<(List<ArticleDto>, int CategoryId)>> GetArticleDtosAndMappedCategoryIdAsync(BaseRequest request)
    {
        var articleDtos = await _dbContext.Articles
            .Where(a => a.ArticleNumber == request.ArticleNumber)
            .Select(article => new ArticleDto(article.Id, article.CharacteristicId))
            .ToListAsync();

        if (articleDtos.Count == 0)
        {
            return ArticleErrors.ArticleNotFound(request.ArticleNumber);
        }

        var mappedCategoryId = await _dbContext.Categories
            .Where(category => category.RootCategoryId == request.RootCategoryId && category.Articles!.Any(article => article.ArticleNumber == request.ArticleNumber))
            .Select(category => (int?)category.Id)
            .SingleOrDefaultAsync();

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
        var setProductTypeId = await _dbContext.AttributeBooleanValues
            .AsNoTracking()
            .Where(value =>
                value.Value
                && articleIds.Contains(value.ArticleId)
                && value.Attribute!.ParentAttributeId == null
                && value.Attribute!.RootCategoryId == rootCategoryId)
            .Select(value => (int?)value.AttributeId)
            .FirstOrDefaultAsync();

        if (setProductTypeId is not null)
        {
            productTypeIds = [.. productTypeIds, setProductTypeId.Value];
        }

        productTypeIds = productTypeIds.Distinct().ToList();

        var attributes = await _dbContext.Attributes.RecursiveCteQuery(
                attribute => productTypeIds.Contains(attribute.Id),
                attribute => attribute.SubAttributes)
            .AsNoTrackingWithIdentityResolution()
            .ToListAsync();

        List<int> attributeIds = attributes.ConvertAll(attribute => attribute.Id);

        var booleanValues = await _dbContext.AttributeBooleanValues
            .Where(value => articleIds.Contains(value.ArticleId) && attributeIds.Contains(value.AttributeId))
            .Select(value => new AttributeValueDto(value.AttributeId, value.ArticleId, value.Value.ToString()))
            .ToListAsync();

        var decimalValues = await _dbContext.AttributeDecimalValues
            .Where(value => articleIds.Contains(value.ArticleId) && attributeIds.Contains(value.AttributeId))
            .Select(value => new AttributeValueDto(value.AttributeId, value.ArticleId, value.Value.ToString(CultureInfo.InvariantCulture)))
            .ToListAsync();

        var intValues = await _dbContext.AttributeIntValues
            .Where(value => articleIds.Contains(value.ArticleId) && attributeIds.Contains(value.AttributeId))
            .Select(value => new AttributeValueDto(value.AttributeId, value.ArticleId, value.Value.ToString(CultureInfo.InvariantCulture)))
            .ToListAsync();

        var stringValues = await _dbContext.AttributeStringValues
            .Where(value => articleIds.Contains(value.ArticleId) && attributeIds.Contains(value.AttributeId))
            .Select(value => new AttributeValueDto(value.AttributeId, value.ArticleId, value.Value))
            .ToListAsync();

        var attributeValueDtos = booleanValues.Concat(decimalValues).Concat(intValues).Concat(stringValues);

        return attributes.ConvertAll(attribute =>
            (attribute, attributeValueDtos.Where(value => value.AttributeId == attribute.Id).ToList()));
    }
}
