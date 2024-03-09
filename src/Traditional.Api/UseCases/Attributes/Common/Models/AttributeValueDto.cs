namespace Traditional.Api.UseCases.Attributes.Common.Models;

/// <summary>
/// Holds the attribute value data.
/// </summary>
/// <param name="AttributeId">The attribute id.</param>
/// <param name="ArticleId">The article id.</param>
/// <param name="Value">The value as string.</param>
public record AttributeValueDto(int AttributeId, int ArticleId, string Value);
