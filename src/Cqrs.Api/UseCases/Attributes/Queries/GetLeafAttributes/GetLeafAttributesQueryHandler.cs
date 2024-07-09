using System.Globalization;
using Cqrs.Api.UseCases.Attributes.Common.Errors;
using Cqrs.Api.UseCases.Attributes.Common.Responses;
using Cqrs.Api.UseCases.Attributes.Common.Services;
using ErrorOr;

namespace Cqrs.Api.UseCases.Attributes.Queries.GetLeafAttributes;

/// <summary>
/// Handles the attribute requests.
/// </summary>
public class GetLeafAttributesQueryHandler(AttributeReadService _attributeReadService, AttributeReadConverter _attributeReadConverter)
{
    /// <summary>
    /// Handles the GET request for category specific leafAttributes.
    /// </summary>
    /// <param name="query">The request.</param>
    /// <returns>A list of category specific leaf attributes of the article in the category tree.</returns>
    public async Task<ErrorOr<List<GetAttributesResponse>>> GetLeafAttributesAsync(GetLeafAttributesQuery query)
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
        return await _attributeReadConverter.ConvertAllLeafAttributes(
            query.ArticleNumber,
            attribute,
            attributeDtos,
            articleDtos);
    }
}
