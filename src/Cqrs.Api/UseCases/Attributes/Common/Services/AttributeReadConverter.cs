using System.Globalization;
using System.Text.RegularExpressions;
using Cqrs.Api.Common.DataAccess.Repositories;
using Cqrs.Api.UseCases.Articles.Persistence.Repositories;
using Cqrs.Api.UseCases.Attributes.Common.Models;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Extensions;
using Cqrs.Api.UseCases.Attributes.Common.Responses;
using Attribute = Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Cqrs.Api.UseCases.Attributes.Common.Services;

/// <summary>
/// Converts database attributes into dtos for the category specifics get endpoint.
/// </summary>
public class AttributeReadConverter(
    IArticleReadRepository _articleReadRepository,
    ICachedReadRepository<AttributeMapping> _attributeMappingReadRepository)
{
    private static readonly string[] ColorMarketplaceAttributeId = ["color"];

    private readonly Dictionary<string, bool> _articleVariantInformation = new(StringComparer.OrdinalIgnoreCase);
    private List<AttributeMapping>? _allAttributeMappings;

    /// <summary>
    /// Converts all leaf attributes of an attribute into dtos for the category specifics get endpoint.
    /// </summary>
    /// <param name="articleNumber">The article number to get the attribute values for.</param>
    /// <param name="attribute">The attribute to convert.</param>
    /// <param name="allAttributes">The attribute values for all attributes.</param>
    /// <param name="articleDtos">The article ids with their characteristic ids.</param>
    /// <returns>The converted dtos for the category specifics get endpoint.</returns>
    public async Task<List<GetAttributesResponse>> ConvertAllLeafAttributes(
        string articleNumber,
        Attribute attribute,
        List<(Attribute Attribute, List<AttributeValueDto> AttributeValueDtos)> allAttributes,
        List<ArticleDto> articleDtos)
    {
        // 1. Get the variant information for the article
        if (!_articleVariantInformation.TryGetValue(articleNumber, out var hasVariants))
        {
            hasVariants = await _articleReadRepository.HasArticleVariantsAsync(articleNumber);
            _articleVariantInformation[articleNumber] = hasVariants;
        }

        // 2. Filter the sub attributes of the attribute
        attribute = await FilterSubAttributes(attribute, hasVariants);
        attribute.SubAttributes ??= [];

        // 3. Convert all sub attributes without further sub attributes
        List<GetAttributesResponse> leafResponses = new(attribute.SubAttributes.Count);
        foreach (var subAttribute in attribute.SubAttributes.FindAll(subAttribute => subAttribute.SubAttributes is null or { Count: 0 }))
        {
            var attributeValueDtos = allAttributes
                .Single(x => x.Attribute.Id == subAttribute.Id)
                .AttributeValueDtos;

            var response = await ConvertAttributeToResponse(articleNumber, subAttribute, attributeValueDtos, articleDtos, allAttributes);
            response.MinValues = subAttribute.GetMinValues(checkParents: false);

            leafResponses.Add(response);
        }

        // 4. Set the dependent attributes for each leaf attribute
        var attributeIds = leafResponses.ConvertAll(response => response.AttributeId);
        leafResponses.ForEach(response => response.DependentAttributes = attributeIds.Except([response.AttributeId]).ToList());

        // 5. Convert all sub attributes until there are no more sub attributes
        List<(Attribute Attribute, List<GetAttributesResponse> leafResponses)> attributesWithResponses = new(attribute.SubAttributes.Count);
        foreach (var subAttribute in attribute.SubAttributes)
        {
            var responseDtos = await ConvertAllLeafAttributes(
                articleNumber,
                subAttribute,
                allAttributes,
                articleDtos);

            attributesWithResponses.Add((subAttribute, responseDtos));
        }

        foreach (var (dbAttribute, responses) in attributesWithResponses)
        {
            if (dbAttribute.GetMinValues(checkParents: false) == 0)
            {
                responses.ForEach(response => response.MinValues = 0);
            }
        }

        // 6. Order the attributes by minValues and flatten the list
        return attributesWithResponses
            .SelectMany(tuple => tuple.leafResponses)
            .Union(leafResponses)
            .ToList();
    }

    /// <summary>
    /// Converts an attribute into a dto for the category specifics get endpoint.
    /// </summary>
    /// <param name="articleNumber">The article number to get the attribute values for.</param>
    /// <param name="attribute">The attribute to convert.</param>
    /// <param name="attributeValueDtos">The attribute values with their article ids.</param>
    /// <param name="articleDtos">The article ids with their characteristic ids.</param>
    /// <param name="otherAttributes">The other attributes to get the values from.</param>
    /// <returns>A dto for the category specifics get endpoint.</returns>
    public async Task<GetAttributesResponse> ConvertAttributeToResponse(
        string articleNumber,
        Attribute attribute,
        List<AttributeValueDto> attributeValueDtos,
        List<ArticleDto> articleDtos,
        List<(Attribute Attribute, List<AttributeValueDto> ArticleIdsWithAttributeValue)>? otherAttributes = null)
    {
        // 1. Get the variant information for the article
        if (!_articleVariantInformation.TryGetValue(articleNumber, out var hasVariants))
        {
            hasVariants = await _articleReadRepository.HasArticleVariantsAsync(articleNumber);
            _articleVariantInformation[articleNumber] = hasVariants;
        }

        // 2. Filter the sub attributes of the attribute
        attribute = await FilterSubAttributes(attribute, hasVariants);
        attribute.SubAttributes ??= [];

        // 3. Fetch the attribute values, product type and set product type
        var variantAttributeValues = GetAttributeValuesPerCharacteristicId(attributeValueDtos, articleDtos);
        var productType = GetProductType(attribute);
        var setProductType = otherAttributes?
            .Where(tuple =>
                tuple.Attribute.ParentAttribute is null
                && tuple.ArticleIdsWithAttributeValue.Exists(dto => string.Equals(dto.Value, "True", StringComparison.OrdinalIgnoreCase)))
            .OrderByDescending(tuple => tuple.ArticleIdsWithAttributeValue.Count(dto => string.Equals(dto.Value, "True", StringComparison.OrdinalIgnoreCase)))
            .FirstOrDefault()
            .Attribute;

        // If the productType of the attribute is not currently set, also return values from a similar attribute in the set productType
        if (productType != attribute
            && productType != setProductType
            && setProductType is not null
            && variantAttributeValues.TrueForAll(value => value.Values.Length == 0))
        {
            var attributeInSetProductType = SubAttributesFlat(setProductType)
                .FirstOrDefault(dbAttribute => string.Equals(RemoveProductType(dbAttribute.MarketplaceAttributeIds), RemoveProductType(attribute.MarketplaceAttributeIds), StringComparison.OrdinalIgnoreCase));

            if (attributeInSetProductType is not null)
            {
                variantAttributeValues = GetAttributeValuesPerCharacteristicId(
                    otherAttributes!.Single(x => x.Attribute.Id == attributeInSetProductType.Id).ArticleIdsWithAttributeValue,
                    articleDtos);
            }
        }

        // 4. Convert the attribute to a dto
        var allowedValues = attribute.GetAllowedValues();
        var exampleValues = string.IsNullOrWhiteSpace(attribute.ExampleValues) ? [] : attribute.ExampleValues.Split(",").ToArray();

        return new GetAttributesResponse(
            AttributeId: attribute.Id,
            AttributeName: attribute.Name,
            Type: attribute.ValueType.ToString().ToUpper(CultureInfo.InvariantCulture),
            MaxValues: attribute.GetMaxValues(),
            MaxLength: attribute.MaxLength,
            // If both MinLength and MaxLength are null, both will be sent as null, but if MinLength is null and MaxLength is not, MinLength will be sent as 0
            MinLength: attribute.MaxLength is null ? attribute.MinLength : attribute.MinLength ?? 0,
            Description: attribute.Description,
            AllowedValues: attribute.ValueType == AttributeValueType.Boolean ? [] : allowedValues,
            ExampleValues: attribute.ValueType == AttributeValueType.Boolean ? [] : exampleValues,
            SubAttributes: attribute.SubAttributes
                .OrderByDescending(dbAttribute => dbAttribute.MinValues)
                .Select(dbAttribute => dbAttribute.Id.ToString(CultureInfo.InvariantCulture))
                .ToList(),
            AttributePath: GetPath(attribute).SkipLast(count: 1).ToList(),
            IsEditable: (attribute.SubAttributes.Count == 0 || attribute.ParentAttribute == null) && attribute.IsEditable)
        {
            Values = variantAttributeValues,
            MinValues = attribute.GetMinValues()
        };
    }

    /// <summary>
    /// Checks if the attributes marketplaceAttributeId matches the correction exactly, starting from top level
    /// excluding the ProductType.
    /// <para>
    /// Example: correction for marketplaceAttributeId `material` matches `HOME,material` but not `HOME,grip,material`.
    /// </para>
    /// </summary>
    private static bool AttributeMatchesCorrection(string attributeId, string correctionAttributeId)
        => Regex.IsMatch(attributeId, "^[^,]+," + correctionAttributeId + '$', RegexOptions.CultureInvariant, TimeSpan.FromMinutes(2));

    private static List<VariantAttributeValues> GetAttributeValuesPerCharacteristicId(
        IReadOnlyCollection<AttributeValueDto> attributeValueDtos,
        List<ArticleDto> articleDtos)
    {
        return articleDtos.ConvertAll(article => new VariantAttributeValues(
            article.CharacteristicId,
            attributeValueDtos
                .Where(attributeValue => attributeValue.ArticleId == article.ArticleId)
                .Select(attributeValue => attributeValue.Value)
                .ToArray()));
    }

    private static string RemoveProductType(string marketplaceAttributeId)
    {
        return marketplaceAttributeId.Contains(',', StringComparison.Ordinal)
            ? marketplaceAttributeId[marketplaceAttributeId.IndexOf(',', StringComparison.Ordinal)..]
            : marketplaceAttributeId;
    }

    private static Attribute GetProductType(Attribute attribute)
    {
        while (true)
        {
            if (attribute.ParentAttribute is null)
            {
                return attribute;
            }

            attribute = attribute.ParentAttribute;
        }
    }

    private static IEnumerable<string> GetPath(Attribute attribute)
    {
        while (attribute.ParentAttribute is not null)
        {
            yield return attribute.ParentAttribute.Name;
            attribute = attribute.ParentAttribute;
        }
    }

    private static IEnumerable<Attribute> SubAttributesFlat(Attribute attribute)
    {
        return attribute.SubAttributes is null
            ? new[] { attribute }
            : attribute.SubAttributes
                .SelectMany(SubAttributesFlat)
                .Prepend(attribute);
    }

    private async Task<Attribute> FilterSubAttributes(Attribute attribute, bool hasVariants)
    {
        if (attribute.SubAttributes == null || attribute.SubAttributes.Count == 0)
        {
            return attribute;
        }

        _allAttributeMappings ??= await _attributeMappingReadRepository.GetAllAsync();
        var baseAttributeIds = _allAttributeMappings.Select(mapping => mapping.AttributeReference.Split(",")[0]);

        if (hasVariants)
        {
            baseAttributeIds = baseAttributeIds.Except(ColorMarketplaceAttributeId, StringComparer.OrdinalIgnoreCase);
        }

        attribute.SubAttributes = attribute.SubAttributes
            .Where(dbAttribute => !baseAttributeIds.Any(baseAttribute => AttributeMatchesCorrection(dbAttribute.MarketplaceAttributeIds, baseAttribute)))
            .ToList();

        return attribute;
    }
}
