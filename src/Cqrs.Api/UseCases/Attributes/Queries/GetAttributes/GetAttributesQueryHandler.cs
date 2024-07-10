using Cqrs.Api.Common.BaseRequests;
using Cqrs.Api.Common.DataAccess.Persistence;
using Cqrs.Api.Common.DataAccess.Repositories;
using Cqrs.Api.UseCases.Attributes.Common.Models;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities;
using Cqrs.Api.UseCases.Attributes.Common.Responses;
using Cqrs.Api.UseCases.Attributes.Common.Services;
using ErrorOr;
using Microsoft.EntityFrameworkCore;
using Attribute = Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Cqrs.Api.UseCases.Attributes.Queries.GetAttributes;

/// <summary>
/// Handles the <see cref="BaseQuery"/> request.
/// </summary>
public class GetAttributesQueryHandler(
    CqrsReadDbContext _dbContext,
    ICachedReadRepository<AttributeMapping> _attributeMappingReadRepository,
    AttributeReadService _attributeReadService)
{
    private const string TRUE_STRING = "true";

    /// <summary>
    /// Handles the GET request for category specific attributes.
    /// </summary>
    /// <param name="query">The request.</param>
    /// <returns>A list of category specific attributes of the article in the category tree.</returns>
    public async Task<ErrorOr<List<GetAttributesResponse>>> GetAttributesAsync(BaseQuery query)
    {
        // 1. Fetch the article DTOs and the mapped category Id
        var dtoOrError = await _attributeReadService.GetArticleDtosAndMappedCategoryIdAsync(query);

        if (dtoOrError.IsError)
        {
            return dtoOrError.Errors;
        }

        var (articleDtos, mappedCategoryId) = dtoOrError.Value;

        // 2. Fetch the attributes with sub attributes and boolean values
        var attributeDtos = await GetAttributesWithSubAttributesAndBooleanValues(
                mappedCategoryId,
                articleDtos.Select(a => a.ArticleId))
            .ToListAsync();

        // 3. Convert the attributes to responses
        List<GetAttributesResponse> responseDtos = new(attributeDtos.Count);
        GetAttributesResponse? attributeWithMostTrueValues = null;
        int mostTrueValues = 0;

        foreach (var attributeDto in attributeDtos)
        {
            var responseDto = AttributeConverter.ConvertAttributeToResponse(
                await _dbContext.Articles.AnyAsync(article => article.ArticleNumber == query.ArticleNumber && article.CharacteristicId > 0),
                attributeDto.Attribute,
                attributeDto.ArticleIdsWithBoolValues,
                articleDtos,
                await _attributeMappingReadRepository.GetAllAsync());

            responseDtos.Add(responseDto);

            var trueValuesCount = responseDto.Values.Count(value => value.Values.Contains(TRUE_STRING, StringComparer.OrdinalIgnoreCase));

            if (trueValuesCount > mostTrueValues || attributeWithMostTrueValues is null)
            {
                attributeWithMostTrueValues = responseDto;
                mostTrueValues = trueValuesCount;
            }
        }

        // 4. Set the values of the attribute with the most true values to true or an empty list if it has no true values
        if (attributeWithMostTrueValues is not null)
        {
            var hasTrueValues = mostTrueValues > 0;
            attributeWithMostTrueValues.Values = articleDtos.ConvertAll(articleDto => new VariantAttributeValues(articleDto.CharacteristicId, hasTrueValues ? [TRUE_STRING] : []));
        }

        // 6. Set the values of the other attributes to an empty list
        foreach (var response in responseDtos.Where(response => response != attributeWithMostTrueValues))
        {
            response.Values = articleDtos.ConvertAll(articleDto => new VariantAttributeValues(articleDto.CharacteristicId, []));
        }

        return responseDtos;
    }

    /// <summary>
    /// Gets the attributes with sub-attributes and boolean values.
    /// </summary>
    /// <param name="categoryId">The category id to get the attributes for.</param>
    /// <param name="articleIds">The article ids to get the attributes for.</param>
    /// <returns>An <see cref="IAsyncEnumerable{T}"/> of a tuple with the <see cref="Attribute"/> and a list of <see cref="AttributeValueDto"/>s.</returns>
    private IAsyncEnumerable<(Attribute Attribute, List<AttributeValueDto> ArticleIdsWithBoolValues)> GetAttributesWithSubAttributesAndBooleanValues(int categoryId, IEnumerable<int> articleIds)
    {
        return _dbContext.Attributes
            .Where(attribute => attribute.Categories!.Any(dbCategory => dbCategory.Id == categoryId) && attribute.ParentAttributeId == null)
            .Include(attribute => attribute.SubAttributes)
            .Select(attribute => new
            {
                Attribute = attribute,
                ArticleIdsWithBoolValues = new List<AttributeValueDto>(attribute.AttributeBooleanValues!
                    .Where(value => articleIds.Contains(value.ArticleId))
                    .Select(value => new AttributeValueDto(value.AttributeId, value.ArticleId, value.Value.ToString())))
            })
            .ToAsyncEnumerable()
            .Select(attribute => (attribute.Attribute, attribute.ArticleIdsWithBoolValues));
    }
}
