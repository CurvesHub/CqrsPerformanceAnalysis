using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Cqrs.Api.UseCases.Articles.Persistence.Entities;
using Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.AttributeValues;
using Cqrs.Api.UseCases.Categories.Common.Persistence.Entities;
using Cqrs.Api.UseCases.RootCategories.Common.Persistence.Entities;
using TestCommon.Constants;
using Attribute = Cqrs.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Cqrs.Tests.TestCommon.Factories;

/// <summary>
/// A factory for creating test <see cref="Attribute"/> instances.
/// </summary>
[SuppressMessage("Design", "S107: Methods should not have too many parameters", Justification = "Required for testing.")]
public static class AttributeFactory
{
    /// <summary>
    /// Creates an instance of a <see cref="Attribute"/>.
    /// </summary>
    /// <param name="name">The attribute's name.</param>
    /// <param name="valueType">The attribute's value type.</param>
    /// <param name="minValues">The attribute's minimum values.</param>
    /// <param name="maxValues">The attribute's maximum values.</param>
    /// <param name="marketplaceAttributeIds">The attribute's marketplace attribute ids.</param>
    /// <param name="allowedValues">The attribute's allowed values.</param>
    /// <param name="minLength">The attribute's minimum length.</param>
    /// <param name="maxLength">The attribute's maximum length.</param>
    /// <param name="productType">The attribute's product type.</param>
    /// <param name="isEditable">A value indicating whether the attribute is editable.</param>
    /// <param name="exampleValues">The attribute's example values.</param>
    /// <param name="description">The attribute's description.</param>
    /// <param name="parentAttribute">The associated parent attribute.</param>
    /// <param name="rootCategory">The associated root category.</param>
    /// <param name="subAttributes">The associated sub attributes.</param>
    /// <param name="categories">The associated categories.</param>
    /// <param name="attributeBooleanValues">The associated attribute boolean values.</param>
    /// <param name="attributeDecimalValues">The associated attribute decimal values.</param>
    /// <param name="attributeIntValues">The associated attribute int values.</param>
    /// <param name="attributeStringValues">The associated attribute string values.</param>
    /// <returns>A new instance of <see cref="Attribute"/></returns>
    public static Attribute CreateAttribute(
        string? name = null,
        AttributeValueType? valueType = null,
        int? minValues = null,
        int? maxValues = null,
        string? marketplaceAttributeIds = null,
        string? allowedValues = null,
        decimal? minLength = null,
        decimal? maxLength = null,
        string? productType = null,
        bool isEditable = true,
        string? exampleValues = null,
        string? description = null,
        Attribute? parentAttribute = null,
        RootCategory? rootCategory = null,
        List<Attribute>? subAttributes = null,
        List<Category>? categories = null,
        List<AttributeBooleanValue>? attributeBooleanValues = null,
        List<AttributeDecimalValue>? attributeDecimalValues = null,
        List<AttributeIntValue>? attributeIntValues = null,
        List<AttributeStringValue>? attributeStringValues = null)
    {
        return new Attribute(
            name ?? TestConstants.Attribute.NAME,
            valueType ?? AttributeValueType.String,
            minValues ?? 1,
            maxValues ?? 1,
            marketplaceAttributeIds ?? string.Empty,
            allowedValues)
        {
            MinLength = minLength,
            MaxLength = maxLength,
            ProductType = productType ?? string.Empty,
            IsEditable = isEditable,
            ExampleValues = exampleValues,
            Description = description,
            ParentAttributeId = parentAttribute?.Id,
            ParentAttribute = parentAttribute,
            Categories = categories,
            RootCategoryId = rootCategory!.Id,
            RootCategory = rootCategory,
            SubAttributes = subAttributes,
            AttributeBooleanValues = attributeBooleanValues,
            AttributeDecimalValues = attributeDecimalValues,
            AttributeIntValues = attributeIntValues,
            AttributeStringValues = attributeStringValues
        };
    }

    /// <summary>
    /// Creates multiple instances of an <see cref="Attribute"/>.
    /// </summary>
    /// <param name="amount">The amount of attributes to create.</param>
    /// <returns>A collection of new <see cref="Attribute"/> instances.</returns>
    public static IEnumerable<Attribute> CreateAttributes(int amount)
    {
        return Enumerable
            .Range(0, amount)
            .Select(index => CreateAttribute(name: $"{TestConstants.Attribute.NAME} {index}"));
    }

    /// <summary>
    /// Creates and adds multiple sub-attributes to the given attribute.
    /// </summary>
    /// <param name="attribute">The attribute to create sub-attributes for.</param>
    /// <param name="amount">The amount of sub-attributes to create.</param>
    /// <param name="valueType">The value type of the sub-attributes.</param>
    /// <param name="optional">Indicates whether the sub-attributes are optional.</param>
    /// <returns>An list of new <see cref="Attribute"/> instances.</returns>
    public static List<Attribute> AddSubAttributesTo(
        Attribute attribute,
        int amount,
        AttributeValueType? valueType = null,
        bool optional = false)
    {
        attribute.SubAttributes ??= [];

        var subAttributes = Enumerable
            .Range(0, amount)
            .Select(index => CreateAttribute(
                name: $"{attribute.Name} Sub {index}",
                valueType: valueType ?? attribute.ValueType,
                minValues: optional ? 0 : 1,
                marketplaceAttributeIds: $"{attribute.MarketplaceAttributeIds},sub {index + attribute.SubAttributes.Count}",
                rootCategory: attribute.RootCategory,
                categories: attribute.Categories))
            .ToList();

        attribute.SubAttributes.AddRange(subAttributes);

        return subAttributes;
    }

    /// <summary>
    /// Creates a string value for an attribute.
    /// </summary>
    /// <param name="attributeValueType">The attribute value type to create a value for.</param>
    /// <returns>A string value for the attribute.</returns>
    /// <exception cref="NotSupportedException">Thrown when the attribute value type is not supported.</exception>
    public static string CreateAttributeValue(AttributeValueType attributeValueType)
    {
        return attributeValueType switch
        {
            AttributeValueType.Boolean => (Random.Shared.Next(2) == 1).ToString(),
            AttributeValueType.Int => "10",
            AttributeValueType.Decimal => "10.01",
            AttributeValueType.String => "TestStringValue",
            _ => throw new NotSupportedException("AttributeValueType not supported.")
        };
    }

    /// <summary>
    /// Adds a value to the given attribute.
    /// </summary>
    /// <param name="attribute">The attribute to add the value to.</param>
    /// <param name="value">The value to add.</param>
    /// <param name="article">The article associated to the value.</param>
    /// <exception cref="NotSupportedException">Thrown when the attribute value type is not supported.</exception>
    public static void AddValueToAttribute(Attribute attribute, string value, Article article)
    {
        attribute.AttributeBooleanValues ??= [];
        attribute.AttributeIntValues ??= [];
        attribute.AttributeDecimalValues ??= [];
        attribute.AttributeStringValues ??= [];

        switch (attribute.ValueType)
        {
            case AttributeValueType.Boolean:
                attribute.AttributeBooleanValues.Add(new AttributeBooleanValue(bool.Parse(value)) { Article = article });
                break;
            case AttributeValueType.Int:
                attribute.AttributeIntValues.Add(new AttributeIntValue(int.Parse(value, CultureInfo.InvariantCulture)) { Article = article });
                break;
            case AttributeValueType.Decimal:
                attribute.AttributeDecimalValues.Add(new AttributeDecimalValue(decimal.Parse(value, CultureInfo.InvariantCulture)) { Article = article });
                break;
            case AttributeValueType.String:
                attribute.AttributeStringValues.Add(new AttributeStringValue(value) { Article = article });
                break;
            default:
                throw new NotSupportedException();
        }
    }

    /// <summary>
    /// Adds a value with the given number to the given attribute.
    /// </summary>
    /// <param name="attribute">The attribute to add the value to.</param>
    /// <param name="valueNumber">The value number.</param>
    /// <param name="article">The article associated to the value.</param>
    /// <exception cref="NotSupportedException">Thrown when the attribute value type is not supported.</exception>
    public static void AddValueWithNumberToAttribute(Attribute attribute, int valueNumber, Article article)
    {
        attribute.AttributeBooleanValues ??= [];
        attribute.AttributeIntValues ??= [];
        attribute.AttributeDecimalValues ??= [];
        attribute.AttributeStringValues ??= [];

        switch (attribute.ValueType)
        {
            case AttributeValueType.Boolean:
                attribute.AttributeBooleanValues.Add(new AttributeBooleanValue(valueNumber % 2 == 0) { Article = article });
                break;
            case AttributeValueType.Int:
                attribute.AttributeIntValues.Add(new AttributeIntValue(valueNumber) { Article = article });
                break;
            case AttributeValueType.Decimal:
                attribute.AttributeDecimalValues.Add(new AttributeDecimalValue(valueNumber + 0.5m) { Article = article });
                break;
            case AttributeValueType.String:
                attribute.AttributeStringValues.Add(new AttributeStringValue("TestStringValue " + valueNumber.ToString(CultureInfo.InvariantCulture)) { Article = article });
                break;
            default:
                throw new NotSupportedException();
        }
    }
}
