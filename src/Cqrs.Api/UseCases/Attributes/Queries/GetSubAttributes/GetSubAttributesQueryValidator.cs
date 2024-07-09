using System.Globalization;
using Cqrs.Api.Common.BaseRequests;
using FluentValidation;
using JetBrains.Annotations;

namespace Cqrs.Api.UseCases.Attributes.Queries.GetSubAttributes;

/// <summary>
/// Defines the validation rules for the <see cref="GetSubAttributesQuery"/>.
/// </summary>
[UsedImplicitly]
public class GetSubAttributesQueryValidator
    : AbstractValidator<GetSubAttributesQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSubAttributesQueryValidator"/> class.
    /// Defines the validation rules for the <see cref="GetSubAttributesQuery"/>.
    /// </summary>
    /// <param name="baseValidator">The validator for the <see cref="BaseQuery"/> class.</param>
    public GetSubAttributesQueryValidator(IValidator<BaseQuery> baseValidator)
    {
        Include(baseValidator);

        RuleFor(request => request.AttributeIds)
            .Must(attributeIds => Array.TrueForAll(
                attributeIds.Split(","),
                s => int.TryParse(s, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out int id)
                     && id > 0))
            .WithMessage("The value of 'Attribute Ids' must be integers separated by comma and each must be greater than '0'.");
    }
}
