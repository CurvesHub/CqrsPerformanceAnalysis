using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Traditional.Api.Common.DataAccess.Persistence;
using Traditional.Api.Common.Extensions;
using Traditional.Api.UseCases.Attributes.Common.Models;
using Attribute = Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Traditional.Api.UseCases.Attributes.Common.Persistence.Repositories;

/// <inheritdoc />
[SuppressMessage("Performance", "MA0020:Use direct methods instead of LINQ methods", Justification = "Not possible with EF Core linq queries")]
internal class AttributeRepository(TraditionalDbContext _dbContext) : IAttributeRepository
{
    /// <inheritdoc />
    public IAsyncEnumerable<Attribute> GetAttributesAndSubAttributesFlatRecursivelyAsNoTracking(IEnumerable<int> attributeIds)
    {
        return _dbContext.Attributes.RecursiveCteQuery(
                attribute => attributeIds.Contains(attribute.Id),
                attribute => attribute.SubAttributes)
            .AsNoTrackingWithIdentityResolution()
            .AsSplitQuery()
            .AsAsyncEnumerable();
    }

    /// <inheritdoc />
    public async ValueTask<IEnumerable<AttributeValueDto>> LoadAttributeValueDataAsync(IEnumerable<int> attributeIds, ICollection<int> articleIds)
    {
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

        return booleanValues.Concat(decimalValues).Concat(intValues).Concat(stringValues);
    }

    /// <inheritdoc />
    public IAsyncEnumerable<(Attribute Attribute, List<AttributeValueDto> ArticleIdsWithBoolValues)> GetAttributesWithSubAttributesAndBooleanValues(int categoryId, IEnumerable<int> articleIds)
    {
        return _dbContext.Attributes
                .AsSplitQuery()
                .AsNoTracking()
                .Where(attribute => attribute.Categories!.Any(dbCategory => dbCategory.Id == categoryId) && attribute.ParentAttributeId == null)
                .Include(attribute => attribute.SubAttributes)
                .Select(attribute => new
                {
                    Attribute = attribute,
                    Values = new List<AttributeValueDto>(attribute.AttributeBooleanValues!
                        .Where(value => articleIds.Contains(value.ArticleId))
                        .Select(value => new AttributeValueDto(value.AttributeId, value.ArticleId, value.Value.ToString())))
                })
                .ToAsyncEnumerable()
                .Select(attribute => (attribute.Attribute, attribute.Values));
    }

    /// <inheritdoc />
    public async Task<int?> GetFirstAttributeIdsForTrueProductTypesByArticleIdsAndRootCategoryId(IEnumerable<int> articleIds, int rootCategoryId)
    {
        return await _dbContext.AttributeBooleanValues
            .AsNoTracking()
            .Where(value =>
                value.Value
                && articleIds.Contains(value.ArticleId)
                && value.Attribute!.ParentAttributeId == null
                && value.Attribute!.RootCategoryId == rootCategoryId)
            .Select(value => value.AttributeId)
            .FirstOrDefaultAsync();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<Attribute> GetAttributesWithSubAttributesByIdOrMpIdAndByRootCategoryId(string productTypeMpId, IEnumerable<int> attributeIds, int rootCategoryId)
    {
        return _dbContext.Attributes
            .Where(attribute =>
                attribute.RootCategoryId == rootCategoryId
                && (attributeIds.Contains(attribute.Id)
                    || attribute.ProductType == productTypeMpId))
            .Include(a => a.SubAttributes)
            .ToAsyncEnumerable();
    }

    /// <inheritdoc />
    public IAsyncEnumerable<string> GetProductTypeMpIdsByAttributeIds(IEnumerable<int> attributeIds)
    {
        return _dbContext.Attributes
            .Where(attribute => attributeIds.Contains(attribute.Id) && attribute.ParentAttributeId == null)
            .Select(attribute => attribute.MarketplaceAttributeIds)
            .ToAsyncEnumerable();
    }
}
