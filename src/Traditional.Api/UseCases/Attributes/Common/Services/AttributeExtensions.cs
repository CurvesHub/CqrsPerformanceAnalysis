using Attribute = Traditional.Api.UseCases.Attributes.Common.Persistence.Entities.Attribute;

namespace Traditional.Api.UseCases.Attributes.Common.Services;

/// <summary>
/// Provides extension methods for the <see cref="Attribute"/> class.
/// </summary>
public static class AttributeExtensions
{
    /// <summary>
    /// Returns the allowedValues of <paramref name="attribute"/> while taking into consideration if one of the articles is a setComponent and if the attribute is the variation_theme.
    /// </summary>
    /// <param name="attribute">The attribute to get the allowedValues from.</param>
    /// <returns>A dto for the category specifics get endpoint.</returns>
    public static string[] GetAllowedValues(this Attribute attribute)
    {
        return string.IsNullOrWhiteSpace(attribute.AllowedValues)
            ? []
            : attribute.AllowedValues.Split(",").ToArray();
    }

    /// <summary>
    /// Returns the max values of the attribute, or the max values of the parentAttribute, if the attribute has max values otherwise the default of '-1'.
    /// </summary>
    /// <param name="attribute">The attribute to get the max values from.</param>
    /// <returns>A dto for the category specifics get endpoint.</returns>
    public static int GetMaxValues(this Attribute attribute)
    {
        while (true)
        {
            if (attribute.MaxValues is not -1)
            {
                return attribute.MaxValues;
            }

            if (attribute.ParentAttribute is null)
            {
                return -1;
            }

            attribute = attribute.ParentAttribute;
        }
    }

    /// <summary>
    /// Returns the minValues of the attribute while taking the attributeCorrections and the parentAttributes minValues into account.
    /// </summary>
    /// <param name="attribute">The attribute to get the minValues from.</param>
    /// <param name="checkParents">True if the parents minValues should be checked.</param>
    /// <returns>A dto for the category specifics get endpoint.</returns>
    public static int GetMinValues(this Attribute attribute, bool checkParents = true)
    {
        if (!checkParents || attribute.ParentAttribute?.ParentAttribute is null)
        {
            return attribute.MinValues;
        }

        return Math.Min(attribute.MinValues, GetMinValues(attribute.ParentAttribute, checkParents));
    }
}
