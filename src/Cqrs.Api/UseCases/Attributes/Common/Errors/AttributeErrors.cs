using System.Globalization;
using ErrorOr;

namespace Cqrs.Api.UseCases.Attributes.Common.Errors;

/// <summary>
/// Defines the attribute errors.
/// </summary>
public static class AttributeErrors
{
    /// <summary>
    /// Produces an error when a required attribute is missing.
    /// </summary>
    /// <param name="parentAttributeId">The parent attribute id.</param>
    /// <param name="sku">The article sku.</param>
    /// <param name="missingAttributeIds">The missing attribute ids.</param>
    /// <returns>A validation <see cref="ErrorOr.Error"/>.</returns>
    public static Error RequiredAttributeMissing(int parentAttributeId, string sku, IEnumerable<int> missingAttributeIds)
        => Error.Validation(
            code: "RequiredAttributeMissing",
            description: $"At least one required sub attribute is missing for attribute with id '{parentAttributeId.ToString(CultureInfo.InvariantCulture)}' and article with sku '{sku}'. Missing attribute ids: '{string.Join(',', missingAttributeIds)}'");

    /// <summary>
    /// Produces an error when duplicate attribute ids were given.
    /// </summary>
    /// <param name="duplicateAttributeIds">The duplicate attribute ids.</param>
    /// <returns>A validation <see cref="ErrorOr.Error"/>.</returns>
    public static Error DuplicateAttributeIds(IEnumerable<int> duplicateAttributeIds)
        => Error.Validation(
            code: "DuplicateAttributeIds",
            description: $"Cannot handle duplicate attribute Ids. At least one attribute Id was sent multiple times. Duplicate attribute ids: '{string.Join(',', duplicateAttributeIds)}'");

    /// <summary>
    /// Produces an error when a value has the wrong type.
    /// </summary>
    /// <param name="attributeId">The attribute id.</param>
    /// <param name="attributeValue">The given attribute value.</param>
    /// <param name="expectedValueType">The expected value type.</param>
    /// <returns>A validation <see cref="ErrorOr.Error"/>.</returns>
    public static Error WrongValueType(int attributeId, string attributeValue, string expectedValueType)
        => Error.Validation(
            code: "WrongValueType",
            description: $"Wrong value type! The value '{attributeValue}' of attribute with id '{attributeId.ToString(CultureInfo.InvariantCulture)}' has an invalid type! Expected: '{expectedValueType}'");

    /// <summary>
    /// Produces an error when not enough values were given.
    /// </summary>
    /// <param name="attributeId">The attribute id.</param>
    /// <param name="actualNumber">The actual number of values.</param>
    /// <param name="expectedNumber">The expected number of values.</param>
    /// <returns>A validation <see cref="ErrorOr.Error"/>.</returns>
    public static Error NotEnoughValues(int attributeId, int actualNumber, int expectedNumber)
        => Error.Validation(
            code: "NotEnoughValues",
            description: $"The attribute with id '{attributeId.ToString(CultureInfo.InvariantCulture)}' has not enough values! Actual Number: '{actualNumber.ToString(CultureInfo.InvariantCulture)}', Expected: '{expectedNumber.ToString(CultureInfo.InvariantCulture)}'");

    /// <summary>
    /// Produces an error when too many values were given.
    /// </summary>
    /// <param name="attributeId">The attribute id.</param>
    /// <param name="actualNumber">The actual number of values.</param>
    /// <param name="expectedNumber">The expected number of values.</param>
    /// <returns>A validation <see cref="ErrorOr.Error"/>.</returns>
    public static Error TooManyValues(int attributeId, int actualNumber, int expectedNumber)
        => Error.Validation(
            code: "TooManyValues",
            description: $"The attribute with id '{attributeId.ToString(CultureInfo.InvariantCulture)}' has too many values! Actual Number: '{actualNumber.ToString(CultureInfo.InvariantCulture)}', Expected: '{expectedNumber.ToString(CultureInfo.InvariantCulture)}'");

    /// <summary>
    /// Produces an error when a value is too long or too high.
    /// </summary>
    /// <param name="attributeId">The attribute id.</param>
    /// <param name="value">The given value.</param>
    /// <param name="expectedLength">The expected length.</param>
    /// <returns>A validation <see cref="ErrorOr.Error"/>.</returns>
    public static Error ValueTooLongOrTooHigh(int attributeId, string value, decimal expectedLength)
        => Error.Validation(
            code: "ValueTooLongOrTooHigh",
            description: $"The attribute with id '{attributeId.ToString(CultureInfo.InvariantCulture)}' has one or more values that are too long/high! Value: '{value}', Expected length/limit: '{expectedLength.ToString(CultureInfo.InvariantCulture)}'");

    /// <summary>
    /// Produces an error when a value is too short or too low.
    /// </summary>
    /// <param name="attributeId">The attribute id.</param>
    /// <param name="value">The given value.</param>
    /// <param name="expectedLength">The expected length.</param>
    /// <returns>A validation <see cref="ErrorOr.Error"/>.</returns>
    public static Error ValueTooShortOrTooLow(int attributeId, string value, decimal expectedLength)
        => Error.Validation(
            code: "ValueTooShortOrTooLow",
            description: $"The attribute with id '{attributeId.ToString(CultureInfo.InvariantCulture)}' has one or more values that are too short/low! Value: '{value}', Expected length/limit: '{expectedLength.ToString(CultureInfo.InvariantCulture)}'");

    /// <summary>
    /// Produces an error when a value is not in the allowed values.
    /// </summary>
    /// <param name="attributeId">The attribute id.</param>
    /// <param name="givenValues">The given values.</param>
    /// <param name="allowedValues">The allowed values.</param>
    /// <returns>A validation <see cref="ErrorOr.Error"/>.</returns>
    public static Error NotInAllowedValues(int attributeId, string[] givenValues, string[] allowedValues)
        => Error.Validation(
            code: "NotInAllowedValues",
            description: $"At least one value given for attribute with id '{attributeId.ToString(CultureInfo.InvariantCulture)}' is not allowed. Expected: '{string.Join(',', allowedValues)}', Given: '{string.Join(',', givenValues)}'");

    /// <summary>
    /// Produces an error when characteristic ids are not found.
    /// </summary>
    /// <param name="attributeId">The attribute id.</param>
    /// <param name="characteristicIdsNotFound">The unknown characteristic ids.</param>
    /// <returns>A not found <see cref="ErrorOr.Error"/>.</returns>
    public static Error CharacteristicIdNotFound(int attributeId, IEnumerable<int> characteristicIdsNotFound)
        => Error.NotFound(
            code: "CharacteristicIdNotFound",
            description: $"At least one characteristicId not found for attribute with id '{attributeId.ToString(CultureInfo.InvariantCulture)}'. Unknown characteristic ids '{string.Join(',', characteristicIdsNotFound)}'");

    /// <summary>
    /// Produces an error when attribute ids are not found.
    /// </summary>
    /// <param name="unknownAttributeIds">The unknown attribute ids.</param>
    /// <param name="rootCategoryId">The root category id.</param>
    /// <returns>A not found <see cref="ErrorOr.Error"/>.</returns>
    public static Error AttributeIdsNotFound(IEnumerable<int> unknownAttributeIds, int? rootCategoryId)
    {
        var description = rootCategoryId is not null
            ? $"At least one attribute id not found for rootCategoryId '{rootCategoryId.Value.ToString(CultureInfo.InvariantCulture)}'. Unknown attribute ids: '{string.Join(',', unknownAttributeIds)}'"
            : $"At least one attribute id not found. Unknown attribute ids: '{string.Join(',', unknownAttributeIds)}'";

        return Error.NotFound(code: "AttributeIdsNotFound", description: description);
    }
}
