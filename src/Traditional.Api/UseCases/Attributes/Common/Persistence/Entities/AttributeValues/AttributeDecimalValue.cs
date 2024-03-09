using Traditional.Api.UseCases.Articles.Persistence.Entities;

namespace Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;

/// <summary>
/// Represents a <see langword="decimal"/> value associated with an <see cref="Article"/> and <see cref="Attribute"/>.
/// </summary>
/// <param name="value">The associated <see langword="decimal"/> value.</param>
public class AttributeDecimalValue(decimal value) : AttributeValue
{
    /// <summary>
    /// Gets or sets the attribute <see langword="decimal"/> value.
    /// </summary>
    public decimal Value { get; set; } = value;
}
