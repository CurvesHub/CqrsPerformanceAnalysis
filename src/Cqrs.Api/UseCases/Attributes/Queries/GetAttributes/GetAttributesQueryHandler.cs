using Cqrs.Api.Common.BaseRequests;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Repositories;
using Cqrs.Api.UseCases.Attributes.Common.Responses;
using Cqrs.Api.UseCases.Attributes.Common.Services;
using ErrorOr;

namespace Cqrs.Api.UseCases.Attributes.Queries.GetAttributes;

/// <summary>
/// Handles the <see cref="BaseQuery"/> request.
/// </summary>
public class GetAttributesQueryHandler(
    AttributeReadService _attributeReadService,
    AttributeReadConverter _attributeReadConverter,
    IAttributeReadRepository _attributeReadRepository)
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
        var attributeDtos = await _attributeReadRepository
            .GetAttributesWithSubAttributesAndBooleanValues(mappedCategoryId, articleDtos.ConvertAll(a => a.ArticleId))
            .ToListAsync();

        // 3. Convert the attributes to responses
        List<GetAttributesResponse> responseDtos = new(attributeDtos.Count);
        GetAttributesResponse? attributeWithMostTrueValues = null;
        int mostTrueValues = 0;

        foreach (var attributeDto in attributeDtos)
        {
            var responseDto = await _attributeReadConverter.ConvertAttributeToResponse(
                query.ArticleNumber,
                attributeDto.Attribute,
                attributeDto.ArticleIdsWithBoolValues,
                articleDtos);

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
}
