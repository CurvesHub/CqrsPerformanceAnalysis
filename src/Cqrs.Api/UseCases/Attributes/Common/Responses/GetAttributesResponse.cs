using System.Diagnostics.CodeAnalysis;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using JetBrains.Annotations;

namespace Cqrs.Api.UseCases.Attributes.Common.Responses;

/// <summary>
/// Represents the response for all category specific attributes endpoints.
/// </summary>
/// <param name="AttributeId">The id of this attribute</param>
/// <param name="AttributeName">The name of this attribute</param>
/// <param name="Type">The type of this attribute's value (e.g. STRING, INT, DECIMAL (with decimal point) see <see cref="AttributeValueType"/>for all possibilities)</param>
/// <param name="MaxValues">The maximum number of values (-1: no maximum)</param>
/// <param name="MaxLength">The maximum length for strings, maximum value for int/decimal, null: no maximum</param>
/// <param name="MinLength">The minimum length for strings, minimum value for int/decimal, null: no minimum</param>
/// <param name="Description">Additional information about the attribute. Null if there is none.</param>
/// <param name="AllowedValues">In case the value of the attribute is restricted to certain values, this array contains all valid values that can be used.
/// If the values are not restricted this array is empty</param>
/// <param name="ExampleValues">An array of example values. If there are no example values, this array is empty.</param>
/// <param name="CanAddAllowedValues">Indicates whether values can be added to the allowedValues</param>
/// <param name="SubAttributes">The attribute's sub attributes, for marketplaces that have a tree-like attribute structure</param>
/// <param name="AttributePath">The names of the attributes in the path to this attribute</param>
/// <param name="IsEditable">Indicating whether the attributes value can be edited after it is set or not.</param>
[SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "Clashed with nullable array declaration")]
[PublicAPI]
public record GetAttributesResponse(
    int AttributeId,
    string AttributeName,
    string Type,
    int MaxValues,
    decimal? MaxLength = default,
    decimal? MinLength = default,
    string? Description = default,
    string[]? AllowedValues = default,
    string[]? ExampleValues = default,
    bool CanAddAllowedValues = false,
    List<string>? SubAttributes = default,
    List<string>? AttributePath = default,
    bool IsEditable = true)
{
    /// <summary>
    /// Gets or sets the values of the attribute variant per characteristic id.
    /// </summary>
    public List<VariantAttributeValues> Values { get; set; } = [];

    /// <summary>
    /// Gets or sets minimum number of values.
    /// 0: optional attribute
    /// >=1: it's a mandatory attribute.
    /// </summary>
    public int MinValues { get; set; }

    /// <summary>
    /// Gets or sets the ids of the attributes that need a value if this attribute has a value.
    /// </summary>
    public List<int> DependentAttributes { get; set; } = [];
}
