using Cqrs.Api.Common.DataAccess.Entities;

namespace Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities;

/// <summary>
/// Entity to store attribute mappings.
/// Describes the attribute mapping for an attribute, i.e. which attributes are fetched from where.
/// </summary>
public class AttributeMapping(string attributeReference, string? mapping = default)
    : BaseEntity
{
    /// <summary>
    /// Gets or sets the reference value to the <see cref="Attribute"/>.
    /// </summary>
    /// <example>
    /// Could be filled with the MarketplaceAttributeId, Name or a different value of the attribute based on marketplace needs.
    /// </example>
    public string AttributeReference { get; set; } = attributeReference;

    /// <summary>
    /// Gets or sets the constant mapping.
    /// </summary>
    public string? Mapping { get; set; } = mapping;
}
