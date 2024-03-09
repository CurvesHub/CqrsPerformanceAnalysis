using Traditional.Api.UseCases.Articles.Persistence.Entities;

namespace Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;

/// <summary>
/// Represents a <see langword="int"/> value associated with an <see cref="Article"/> and <see cref="Attribute"/>.
/// </summary>
/// <param name="value">The associated <see langword="int"/> value.</param>
public class AttributeIntValue(int value) : AttributeValue
{
    /// <summary>
    /// Gets or sets the attribute <see langword="int"/> value.
    /// </summary>
    public int Value { get; set; } = value;
}
