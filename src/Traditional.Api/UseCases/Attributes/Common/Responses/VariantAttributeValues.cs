namespace Traditional.Api.UseCases.Attributes.Common.Responses;

/// <summary>
/// Holds the attribute values for a variant article.
/// </summary>
/// <param name="CharacteristicId">The characteristic id.</param>
/// <param name="Values">The values for the characteristic.</param>
public record VariantAttributeValues(int CharacteristicId, string[] Values);
