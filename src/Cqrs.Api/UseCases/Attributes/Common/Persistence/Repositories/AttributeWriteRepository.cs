using System.Diagnostics.CodeAnalysis;
using Cqrs.Api.Common.DataAccess.Persistence;
using Microsoft.EntityFrameworkCore;
using Attribute = Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Cqrs.Api.UseCases.Attributes.Common.Persistence.Repositories;

/// <inheritdoc />
[SuppressMessage("Performance", "MA0020:Use direct methods instead of LINQ methods", Justification = "Not possible with EF Core linq queries")]
internal class AttributeWriteRepository(CqrsWriteDbContext _dbContext) : IAttributeWriteRepository
{
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
