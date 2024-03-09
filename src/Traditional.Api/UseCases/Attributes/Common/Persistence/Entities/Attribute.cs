using Traditional.Api.Common.DataAccess.Entities;
using Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Traditional.Api.UseCases.Categories.Common.Persistence.Entities;
using Traditional.Api.UseCases.RootCategories.Common.Persistence.Entities;

namespace Traditional.Api.UseCases.Attributes.Common.Persistence.Entities;

/// <summary>
/// Entity to store attributes.
/// </summary>
/// <param name="name">Sets the associated display name.</param>
/// <param name="valueType">Sets the associated type.</param>
/// <param name="minValues">Sets the minimum attribute values that must be supplied.</param>
/// <param name="maxValues">Sets the maximum attribute values that can be supplied.</param>
/// <param name="marketplaceAttributeIds">Sets the marketplace attribute id.</param>
/// <param name="allowedValues">Sets allowed values to choose from as json string list.</param>
public class Attribute(
    string name,
    AttributeValueType valueType,
    int minValues,
    int maxValues,
    string marketplaceAttributeIds,
    string? allowedValues = null)
    : BaseEntity
{
    /// <summary>
    /// Gets or sets the associated display name.
    /// </summary>
    public string Name { get; set; } = name;

    /// <summary>
    /// Gets or sets the associated type.
    /// </summary>
    public AttributeValueType ValueType { get; set; } = valueType;

    /// <summary>
    /// Gets or sets the minimum attribute values that must be supplied.
    /// </summary>
    /// <remarks>A number greater than zero means the attribute is an required attribute.</remarks>
    public int MinValues { get; set; } = minValues;

    /// <summary>
    /// Gets or sets the maximum attribute values that can be supplied.
    /// </summary>
    /// <remarks>A number of -1 means no maximum number is defined.</remarks>
    public int MaxValues { get; set; } = maxValues;

    /// <summary>
    /// Gets or sets the marketplace attribute id.
    /// </summary>
    public string MarketplaceAttributeIds { get; set; } = marketplaceAttributeIds;

    /// <summary>
    /// Gets or sets allowed values to choose from as json string list.
    /// </summary>
    public string? AllowedValues { get; set; } = allowedValues;

    /// <summary>
    /// Gets or sets the min value, if there is none, it is set to <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// Min number if the type is <see cref="AttributeValueType.Int"/> or <see cref="AttributeValueType.Decimal"/>.
    /// <para>Min length of a string if the type is <see cref="AttributeValueType.String"/>.</para>
    /// </remarks>
    public decimal? MinLength { get; set; }

    /// <summary>
    /// Gets or sets the max value, if there is none, it is set to <see langword="null"/>.
    /// </summary>
    /// <remarks>
    /// Max number if the type is <see cref="AttributeValueType.Int"/> or <see cref="decimal"/>.
    /// <para>Max length of a string if the type is <see cref="AttributeValueType.String"/>.</para>
    /// </remarks>
    public decimal? MaxLength { get; set; }

    /// <summary>
    /// Gets or sets the associated product type.
    /// </summary>
    public string ProductType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the attribute is init only on the marketplace.
    /// </summary>
    public bool IsEditable { get; set; } = true;

    /// <summary>
    /// Gets or sets examples for associated possible values, comma separated.
    /// </summary>
    public string? ExampleValues { get; set; }

    /// <summary>
    /// Gets or sets the associated description.
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Gets or sets the id of the associated parent <see cref="Attribute"/>.
    /// </summary>
    public int? ParentAttributeId { get; set; }

    /// <summary>
    /// Gets or sets the associated parent attribute.
    /// </summary>
    public Attribute? ParentAttribute { get; set; }

    /// <summary>
    /// Gets or sets a collection of associated sub attributes.
    /// </summary>
    /// <remarks>This collection can be used in case a marketplace has tree-like structured attributes.</remarks>
    public List<Attribute>? SubAttributes { get; set; }

    /// <summary>
    /// Gets or sets a collection of associated categories.
    /// </summary>
    public List<Category>? Categories { get; set; }

    /// <summary>
    /// Gets or sets the id of the associated <see cref="RootCategory"/>.
    /// </summary>
    public int RootCategoryId { get; set; }

    /// <summary>
    /// Gets or sets the associated root category.
    /// </summary>
    public RootCategory? RootCategory { get; set; }

    /// <summary>
    /// Gets or sets a collection of associated <see cref="AttributeBooleanValues"/>.
    /// </summary>
    public List<AttributeBooleanValue>? AttributeBooleanValues { get; set; }

    /// <summary>
    /// Gets or sets a collection of associated <see cref="AttributeDecimalValue"/>.
    /// </summary>
    public List<AttributeDecimalValue>? AttributeDecimalValues { get; set; }

    /// <summary>
    /// Gets or sets a collection of associated <see cref="AttributeIntValue"/>.
    /// </summary>
    public List<AttributeIntValue>? AttributeIntValues { get; set; }

    /// <summary>
    /// Gets or sets a collection of associated <see cref="AttributeStringValue"/>.
    /// </summary>
    public List<AttributeStringValue>? AttributeStringValues { get; set; }
}
