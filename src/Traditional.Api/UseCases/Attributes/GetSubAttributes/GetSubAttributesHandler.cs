using ErrorOr;
using Traditional.Api.UseCases.Attributes.Common.Errors;
using Traditional.Api.UseCases.Attributes.Common.Responses;
using Traditional.Api.UseCases.Attributes.Common.Services;

namespace Traditional.Api.UseCases.Attributes.GetSubAttributes;

/// <summary>
/// Handles the attribute requests.
/// </summary>
public class GetSubAttributesHandler(AttributeService _attributeService, AttributeConverter _attributeConverter)
{
    /// <summary>
    /// Handles the GET request for category specific subAttributes.
    /// </summary>
    /// <param name="request">The request.</param>
    /// <returns>A list of the configurations and the ids of the sub-attributes of all passed attributes and their values for the requested article in the requested category tree.</returns>
    public async Task<ErrorOr<List<GetAttributesResponse>>> GetSubAttributesAsync(GetSubAttributesRequest request)
    {
        // 1. Fetch the article DTOs
        var dtoOrError = await _attributeService.GetArticleDtosAndMappedCategoryIdAsync(request);

        if (dtoOrError.IsError)
        {
            return dtoOrError.Errors;
        }

        var (articleDtos, _) = dtoOrError.Value;

        // 2. Parse the attribute ids from the request and get the attributes
        var attributeIds = request.AttributeIds.Split(",").Select(int.Parse).ToList();

        var attributeDtos = await _attributeService.GetAttributesAndSubAttributesWithValuesAsync(
                articleDtos.ConvertAll(articleDto => articleDto.ArticleId),
                request.RootCategoryId,
                attributeIds);

        // 3. Get the unknown attribute ids and return an error if there are any
        var unknownAttributeIds = attributeIds
            .Except(attributeDtos.Select(tuple => tuple.Attribute.Id))
            .ToList();

        if (unknownAttributeIds.Count is not 0)
        {
            return AttributeErrors.AttributeIdsNotFound(unknownAttributeIds, request.RootCategoryId);
        }

        // 4. Convert the attributes to responses and return them
        List<GetAttributesResponse> responseDtos = new(attributeIds.Count);
        foreach (var tuple in attributeIds.Select(attributeId => attributeDtos.First(tuple => tuple.Attribute.Id == attributeId)))
        {
            var responseDto = await _attributeConverter.ConvertAttributeToResponse(
                request.ArticleNumber,
                tuple.Attribute,
                tuple.AttributeValueDtos,
                articleDtos);

            responseDtos.Add(responseDto);
        }

        return responseDtos;
    }
}
