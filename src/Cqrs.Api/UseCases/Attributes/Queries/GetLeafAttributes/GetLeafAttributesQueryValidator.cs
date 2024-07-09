using System.Globalization;
using Cqrs.Api.Common.BaseRequests;
using FluentValidation;
using JetBrains.Annotations;

namespace Cqrs.Api.UseCases.Attributes.Queries.GetLeafAttributes;

/// <summary>
/// Defines the validation rules for the <see cref="GetLeafAttributesQuery"/>.
/// </summary>
[UsedImplicitly]
public class GetLeafAttributesQueryValidator
    : AbstractValidator<GetLeafAttributesQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetLeafAttributesQueryValidator"/> class.
    /// Defines the validation rules for the <see cref="GetLeafAttributesQuery"/>.
    /// </summary>
    /// <param name="baseValidator">The validator for the <see cref="BaseQuery"/> class.</param>
    public GetLeafAttributesQueryValidator(IValidator<BaseQuery> baseValidator)
    {
        Include(baseValidator);

        RuleFor(request => request.AttributeId)
            .Must(attributeId => int.TryParse(attributeId, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out int id) && id > 0)
            .WithMessage("The value of 'Attribute Id' must be greater than '0'.");
    }
}
