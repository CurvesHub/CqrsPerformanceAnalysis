namespace Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;

/// <summary>
/// Represents the type of an attribute value.
/// </summary>
public enum AttributeValueType
{
    /// <summary>
    /// Value is of type <see langword="bool"/>
    /// </summary>
    Boolean,

    /// <summary>
    /// Value is of type <see langword="decimal"/>
    /// </summary>
    Decimal,

    /// <summary>
    /// Value is of type <see langword="int"/>
    /// </summary>
    Int,

    /// <summary>
    /// Value is of type <see langword="string"/>
    /// </summary>
    String
}
