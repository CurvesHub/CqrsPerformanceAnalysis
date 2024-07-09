using System.Globalization;
using ErrorOr;
using Traditional.Api.Common.DataAccess.Repositories;
using Traditional.Api.UseCases.Attributes.Common.Errors;
using Traditional.Api.UseCases.Attributes.Common.Extensions;
using Traditional.Api.UseCases.Attributes.Common.Models;
using Traditional.Api.UseCases.Attributes.Common.Persistence.Entities;
using Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Attribute = Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Traditional.Api.UseCases.Attributes.UpdateAttributeValues;

/// <summary>
/// Checks if the attributes given by the category specifics put endpoint are valid.
/// </summary>
public class NewAttributeValueValidationService(ICachedRepository<AttributeMapping> _attributeMappingRepository)
{
    private List<AttributeMapping>? _allAttributeMappings;

    /// <summary>
    /// Checks if the attributes given by the category specifics put endpoint are valid.
    /// </summary>
    /// <param name="articleNumber">The article number.</param>
    /// <param name="attributes">The attributes to check against.</param>
    /// <param name="newAttributeValues">The new attribute values to check.</param>
    /// <param name="articleDtos">The article ids with characteristic ids.</param>
    /// <returns>An error or success.</returns>
    public async Task<ErrorOr<Success>> ValidateAttributes(
        string articleNumber,
        List<Attribute> attributes,
        List<NewAttributeValue> newAttributeValues,
        List<ArticleDto> articleDtos)
    {
        // 1. Get the characteristic ids from the article DTOs
        var characteristicIds = articleDtos.ConvertAll(article => article.CharacteristicId);

        // 2. Check if the characteristic ids are in the new attribute values
        foreach (var newAttributeValue in newAttributeValues)
        {
            var variantAttributeValues = newAttributeValue.InnerValues
                .Where(innerValue => characteristicIds.TrueForAll(characteristicId => characteristicId != innerValue.CharacteristicId))
                .ToList();

            // If the characteristic ids are not in the new attribute values, return an error
            if (variantAttributeValues.Count is not 0)
            {
                return AttributeErrors.CharacteristicIdNotFound(
                    newAttributeValue.AttributeId,
                    variantAttributeValues.Select(iv => iv.CharacteristicId));
            }
        }

        // 3. Get the attribute ids from the new attribute values
        var attributeIds = newAttributeValues.ConvertAll(newAttributeValue => newAttributeValue.AttributeId);

        // If there are duplicate attribute ids, return an error
        if (HasDuplicates(attributeIds))
        {
            return AttributeErrors.DuplicateAttributeIds(
                attributeIds
                    .GroupBy(id => id)
                    .Where(group => group.Skip(1).Any())
                    .Select(group => group.Key));
        }

        // 4. Check if any of the attribute ids are unknown
        var unknownAttributeIds = attributeIds
            .Except(attributes.Select(attribute => attribute.Id))
            .ToList();

        if (unknownAttributeIds.Count is not 0)
        {
            return AttributeErrors.AttributeIdsNotFound(unknownAttributeIds, rootCategoryId: null);
        }

        // 5. Validate the values of the new attribute values
        var errors = newAttributeValues
            .Select(newAttributeValue => ValidateValues(newAttributeValue, attributes))
            .Where(result => result.IsError)
            .SelectMany(result => result.Errors)
            .ToList();

        if (errors.Count is not 0)
        {
            return errors;
        }

        // 6. Check the required sub attributes
        foreach (var productType in attributes.Where(attribute => attribute.ParentAttribute is null))
        {
            foreach (var characteristicId in characteristicIds)
            {
                var attributesIdsForCharacteristicId = newAttributeValues
                    .Where(newAttributeValue => newAttributeValue.InnerValues
                        .Exists(innerValue => innerValue.CharacteristicId == characteristicId))
                    .Select(newAttributeValue => newAttributeValue.AttributeId)
                    .ToList();

                if (attributesIdsForCharacteristicId.Count is 0 || !attributesIdsForCharacteristicId.Contains(productType.Id))
                {
                    continue;
                }

                var firstInnerValue = newAttributeValues
                    .Find(newAttributeValue => newAttributeValue.AttributeId == productType.Id)!
                    .InnerValues
                    .Find(innerValue => innerValue.CharacteristicId == characteristicId)!
                    .Values[0];

                if (!string.Equals(firstInnerValue, "True", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var result = await CheckRequiredSubAttributes(
                    productType,
                    attributesIdsForCharacteristicId,
                    characteristicId,
                    articleNumber);

                if (result.IsError)
                {
                    errors.AddRange(result.Errors);
                }
            }
        }

        return errors.Count is 0 ? Result.Success : errors;
    }

    private static bool HasDuplicates(List<int> attributeIds)
    {
        var seen = new HashSet<int>(attributeIds.Count);
        return attributeIds.Exists(id => !seen.Add(id));
    }

    private static bool HasReceivedSubAttributesValues(Attribute attribute, ICollection<int> receivedAttributeIds)
    {
        return attribute.SubAttributes is not null
               && (attribute.SubAttributes.Exists(dbAttribute =>
                       dbAttribute.SubAttributes is null or { Count: 0 }
                       && receivedAttributeIds.Contains(dbAttribute.Id))
                   || attribute.SubAttributes.Exists(dbAttribute =>
                       dbAttribute.SubAttributes is null or { Count: 0 }
                       && HasReceivedSubAttributesValues(dbAttribute, receivedAttributeIds)));
    }

    private static decimal GetValueLength(string value, AttributeValueType attributeValueType)
    {
        return attributeValueType switch
        {
            AttributeValueType.String => value.Length,
            AttributeValueType.Int => int.Parse(value, CultureInfo.InvariantCulture),
            AttributeValueType.Decimal => decimal.Parse(value, CultureInfo.InvariantCulture),
            AttributeValueType.Boolean => 1,
            _ => throw new NotSupportedException()
        };
    }

    private static ErrorOr<Success> ValidateValues(
        NewAttributeValue newAttributeValue,
        List<Attribute> attributes)
    {
        // 1. Get the first attribute that matches the attribute id
        var attribute = attributes.First(attribute => attribute.Id == newAttributeValue.AttributeId);

        // If the attribute has sub attributes and is not a base attribute, return Success
        if (attribute.SubAttributes!.Count is not 0 && attribute.ParentAttribute is not null)
        {
            return Result.Success;
        }

        // 2. Check if the values are within the min values
        var minValues = attribute.GetMinValues();
        if (newAttributeValue.InnerValues.Exists(innerValue => innerValue.Values.Length < minValues))
        {
            return AttributeErrors.NotEnoughValues(
                attribute.Id,
                newAttributeValue.InnerValues.First(innerValue => innerValue.Values.Length < minValues).Values.Length,
                minValues);
        }

        // 3. Check if the values are within the max values
        var maxValues = attribute.GetMaxValues();
        if (maxValues is not -1 && newAttributeValue.InnerValues.Exists(innerValue => innerValue.Values.Length > maxValues))
        {
            return AttributeErrors.TooManyValues(
                attribute.Id,
                newAttributeValue.InnerValues.First(innerValue => innerValue.Values.Length > maxValues).Values.Length,
                maxValues);
        }

        // 4. Check if the values are of the correct type
        var givenValues = newAttributeValue.InnerValues
            .SelectMany(innerValue => innerValue.Values)
            .ToList();

        var invalidValue = attribute.ValueType switch
        {
            AttributeValueType.Boolean => givenValues.Find(s => !bool.TryParse(s, out _)),
            AttributeValueType.Int => givenValues.Find(s => !int.TryParse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out _)),
            AttributeValueType.Decimal => givenValues.Find(s => !decimal.TryParse(s, NumberStyles.Float, NumberFormatInfo.InvariantInfo, out _)),
            AttributeValueType.String => null,
            _ => throw new NotSupportedException()
        };

        if (invalidValue is not null)
        {
            return AttributeErrors.WrongValueType(
                attribute.Id,
                invalidValue,
                attribute.ValueType.ToString());
        }

        // 5. Check if the values are within the min length
        string? outOfBoundsValue;
        if (attribute.MinLength is not null)
        {
            outOfBoundsValue = givenValues.Find(value => GetValueLength(value, attribute.ValueType) < attribute.MinLength);

            if (outOfBoundsValue is not null)
            {
                return AttributeErrors.ValueTooShortOrTooLow(
                    attribute.Id,
                    outOfBoundsValue,
                    attribute.MinLength.Value);
            }
        }

        // 6. Check if the values are within the max length
        if (attribute.MaxLength is not null)
        {
            outOfBoundsValue = givenValues
                .Find(value => GetValueLength(value, attribute.ValueType) > attribute.MaxLength);

            if (outOfBoundsValue is not null)
            {
                return AttributeErrors.ValueTooLongOrTooHigh(
                    attribute.Id,
                    outOfBoundsValue,
                    attribute.MaxLength.Value);
            }
        }

        // 7. Check if the values are within the allowed values
        var allowedValues = attribute.GetAllowedValues();

        if (attribute.ValueType is not AttributeValueType.Boolean
            && allowedValues.Length is not 0
            && givenValues.Exists(value => !allowedValues.Contains(value, StringComparer.OrdinalIgnoreCase)))
        {
            return AttributeErrors.NotInAllowedValues(
                attribute.Id,
                givenValues.ToArray(),
                allowedValues);
        }

        return Result.Success;
    }

    private async Task<ErrorOr<Success>> CheckRequiredSubAttributes(
        Attribute attribute,
        List<int> receivedAttributeIds,
        int characteristicId,
        string articleNumber,
        bool attributeIsOptional = false)
    {
        // 1. If the attribute has no sub attributes, return Success
        if (attribute.SubAttributes == null || attribute.SubAttributes.Count == 0)
        {
            return Result.Success;
        }

        // 2. If the attribute is optional and no sub attributes are sent, return Success
        if (attributeIsOptional && !HasReceivedSubAttributesValues(attribute, receivedAttributeIds))
        {
            return Result.Success;
        }

        // 3. Get all attribute mappings and filter the sub attributes
        _allAttributeMappings ??= await _attributeMappingRepository.GetAllAsync();
        var baseAttributeIds = _allAttributeMappings.Select(mapping => mapping.AttributeReference.Split(",")[0]);

        var filteredSubAttributes = attribute.SubAttributes
            .Where(a => !baseAttributeIds.Contains(a.MarketplaceAttributeIds.Split(",")[^1], StringComparer.OrdinalIgnoreCase))
            .ToList();

        // 4. Get the required and optional sub attributes
        var required = filteredSubAttributes.Where(dbAttribute => dbAttribute.GetMinValues() > 0).ToList();
        var optional = filteredSubAttributes.Where(dbAttribute => dbAttribute.GetMinValues() == 0).ToList();

        // 5. Check if the required sub attributes are in the received attribute ids
        var missing = required
            .Where(x => x.SubAttributes is null or { Count: 0 })
            .Select(x => x.Id)
            .Where(id => !receivedAttributeIds.Contains(id))
            .ToList();

        if (missing.Count is not 0)
        {
            return AttributeErrors.RequiredAttributeMissing(
                attribute.Id,
                articleNumber + "_" + characteristicId,
                missing);
        }

        // 6. Check if the required sub attributes are valid
        List<Error> errors = [];
        foreach (var dbAttribute in required.Where(dbAttribute => dbAttribute.SubAttributes is not { Count: 0 }))
        {
            var result = await CheckRequiredSubAttributes(dbAttribute, receivedAttributeIds, characteristicId, articleNumber);

            if (result.IsError)
            {
                errors.AddRange(result.Errors);
            }
        }

        // 7. Check if the optional sub attributes are valid
        foreach (var dbAttribute in optional)
        {
            var result = await CheckRequiredSubAttributes(dbAttribute, receivedAttributeIds, characteristicId, articleNumber, attributeIsOptional: true);
            if (result.IsError)
            {
                errors.AddRange(result.Errors);
            }
        }

        return errors.Count is not 0 ? errors : Result.Success;
    }
}
