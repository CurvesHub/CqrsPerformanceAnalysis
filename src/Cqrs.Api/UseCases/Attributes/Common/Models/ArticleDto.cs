namespace Cqrs.Api.UseCases.Attributes.Common.Models;

/// <summary>
/// Holds the article id and the characteristic id of an article.
/// </summary>
/// <param name="ArticleId">The article id.</param>
/// <param name="CharacteristicId">The characteristic id.</param>
public record ArticleDto(int ArticleId, int CharacteristicId);
