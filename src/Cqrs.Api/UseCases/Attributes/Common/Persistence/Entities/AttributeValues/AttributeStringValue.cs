using Cqrs.Api.UseCases.Articles.Persistence.Entities;

namespace Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;

/// <summary>
/// Represents a <see langword="string"/> value associated with an <see cref="Article"/> and <see cref="Attribute"/>.
/// </summary>
/// <param name="value">The associated <see langword="string"/> value.</param>
public class AttributeStringValue(string value) : AttributeValue
{
    /// <summary>
    /// Gets or sets the attribute <see langword="string"/> value.
    /// </summary>
    public string Value { get; set; } = value;
}
