using Cqrs.Api.UseCases.Attributes.Common.Responses;

namespace Cqrs.Api.UseCases.Attributes.Commands.UpdateAttributeValues;

/// <summary>
/// Represents a new value to set for an attribute.
/// </summary>
/// <param name="AttributeId">The id of the attribute the value will be set to.</param>
/// <param name="InnerValues">The values to set.</param>
public record NewAttributeValue(
    int AttributeId,
    List<VariantAttributeValues> InnerValues);
