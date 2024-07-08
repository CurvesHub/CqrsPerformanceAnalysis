using System.Diagnostics.CodeAnalysis;
using Cqrs.Api.UseCases.Articles.Persistence.Entities;

namespace Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;

/// <summary>
/// Represents a <see langword="bool"/> value associated with an <see cref="Article"/> and <see cref="Attribute"/>.
/// </summary>
/// <param name="value">The associated <see langword="bool"/> value.</param>
[SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1623:Property summary documentation should match accessors", Justification = "Doesn't make sense in this context.")]
public class AttributeBooleanValue(bool value) : AttributeValue
{
    /// <summary>
    /// Gets or sets the attribute <see langword="bool"/> value.
    /// </summary>
    public bool Value { get; set; } = value;
}
