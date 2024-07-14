using System.Globalization;
using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.Common.DataAccess.Repositories;
using Cqrs.Api.UseCases.Attributes.Common.Errors;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities;
using Cqrs.Api.UseCases.Attributes.Common.Responses;
using Cqrs.Api.UseCases.Attributes.Common.Services;
using ErrorOr;
using MediatR;
using Microsoft.EntityFrameworkCore;

namespace Cqrs.Api.UseCases.Attributes.Queries.GetLeafAttributes;

/// <summary>
/// Handles the attribute requests.
/// </summary>
public class GetLeafAttributesQueryHandler(
    CqrsReadDbContext _dbContext,
    ICachedReadRepository<AttributeMapping> _attributeMappingReadRepository,
    AttributeReadService _attributeReadService) : IRequestHandler<GetLeafAttributesQuery, ErrorOr<List<GetAttributesResponse>>>
{
    /// <summary>
    /// Handles the GET request for category specific leafAttributes.
    /// </summary>
    /// <param name="query">The request.</param>
    /// <param name="cancellationToken">The token to cancel the requests.</param>
    /// <returns>A list of category specific leaf attributes of the article in the category tree.</returns>
    public async Task<ErrorOr<List<GetAttributesResponse>>> Handle(GetLeafAttributesQuery query, CancellationToken cancellationToken)
    {
        // 1. Fetch the article DTOs
        var dtoOrError = await _attributeReadService.GetArticleDtosAndMappedCategoryIdAsync(query);

        if (dtoOrError.IsError)
        {
            return dtoOrError.Errors;
        }

        var (articleDtos, _) = dtoOrError.Value;

        // 2. Parse the attribute id from the request and get the attribute
        var attributeId = int.Parse(query.AttributeId, CultureInfo.InvariantCulture);

        var attributeDtos = await _attributeReadService.GetAttributesAndSubAttributesWithValuesAsync(
                articleDtos.ConvertAll(a => a.ArticleId),
                query.RootCategoryId,
                [attributeId]);

        // 4. Get the requested attribute and return an error if it is unknown
        var attribute = attributeDtos.Select(tuple => tuple.Attribute).FirstOrDefault(x => x.Id == attributeId);

        if (attribute is null)
        {
            return AttributeErrors.AttributeIdsNotFound([attributeId], query.RootCategoryId);
        }

        // 4. Convert the attribute to a response and return it
        return AttributeConverter.ConvertAllLeafAttributes(
            await _dbContext.Articles.AnyAsync(article => article.ArticleNumber == query.ArticleNumber && article.CharacteristicId > 0),
            attribute,
            attributeDtos,
            articleDtos,
            await _attributeMappingReadRepository.GetAllAsync());
    }
}
