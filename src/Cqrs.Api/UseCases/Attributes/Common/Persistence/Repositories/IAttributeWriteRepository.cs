using Attribute = Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Cqrs.Api.UseCases.Attributes.Common.Persistence.Repositories;

/// <summary>
/// The repository for <see cref="Attribute"/>.
/// </summary>
public interface IAttributeWriteRepository
{
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
