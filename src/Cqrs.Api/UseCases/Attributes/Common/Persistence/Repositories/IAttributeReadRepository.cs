using Cqrs.Api.UseCases.Attributes.Common.Models;
using Attribute = Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Cqrs.Api.UseCases.Attributes.Common.Persistence.Repositories;

/// <summary>
/// The repository for <see cref="Attribute"/>.
/// </summary>
public interface IAttributeReadRepository
{
    /// <summary>
    /// Gets the leaf attributes recursively.
    /// </summary>
    /// <param name="attributeIds">The attribute ids to get the sub-attributes for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of <see cref="Attribute"/>s.</returns>
    IAsyncEnumerable<Attribute> GetAttributesAndSubAttributesFlatRecursivelyAsNoTracking(IEnumerable<int> attributeIds);

    /// <summary>
    /// Returns the attribute values for the given <paramref name="attributeIds"/> and <paramref name="articleIds"/>.
    /// </summary>
    /// <param name="attributeIds">The attributes to get the values for.</param>
    /// <param name="articleIds">The article ids to get the values for.</param>
    /// <returns>An <see cref="IEnumerable{T}"/> of <see cref="AttributeValueDto"/>s.</returns>
    ValueTask<IEnumerable<AttributeValueDto>> LoadAttributeValueDataAsync(IEnumerable<int> attributeIds, ICollection<int> articleIds);

    /// <summary>
    /// Gets the attributes with sub-attributes and boolean values.
    /// </summary>
    /// <param name="categoryId">The category id to get the attributes for.</param>
    /// <param name="articleIds">The article ids to get the attributes for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of a tuple with the <see cref="Attribute"/> and a list of <see cref="AttributeValueDto"/>s.</returns>
    IAsyncEnumerable<(Attribute Attribute, List<AttributeValueDto> ArticleIdsWithBoolValues)> GetAttributesWithSubAttributesAndBooleanValues(int categoryId, IEnumerable<int> articleIds);

    /// <summary>
    /// Gets the first attribute ids for the true product types by the given <paramref name="articleIds"/> and <paramref name="rootCategoryId"/>.
    /// </summary>
    /// <param name="articleIds">The article ids to get the attribute ids for.</param>
    /// <param name="rootCategoryId">The root category id to get the attribute ids for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of attribute ids.</returns>
    Task<int?> GetFirstAttributeIdsForTrueProductTypesByArticleIdsAndRootCategoryId(IEnumerable<int> articleIds, int rootCategoryId);

    /// <summary>
    /// Gets the attributes with sub-attributes by the given <paramref name="productTypeMpId"/>, <paramref name="attributeIds"/> and <paramref name="rootCategoryId"/>.
    /// </summary>
    /// <param name="productTypeMpId">The product type marketplace ids to get the attributes for.</param>
    /// <param name="attributeIds">The attribute ids to get the attributes for.</param>
    /// <param name="rootCategoryId">The root category id to get the attributes for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of <see cref="Attribute"/>s.</returns>
    IAsyncEnumerable<Attribute> GetAttributesWithSubAttributesByIdOrMpIdAndByRootCategoryId(string productTypeMpId, IEnumerable<int> attributeIds, int rootCategoryId);

    /// <summary>
    /// Gets the product type marketplace ids by the given <paramref name="attributeIds"/>.
    /// </summary>
    /// <param name="attributeIds">The attribute ids to get the product type marketplace ids for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of product type marketplace ids.</returns>
    IAsyncEnumerable<string> GetProductTypeMpIdsByAttributeIds(IEnumerable<int> attributeIds);
}
