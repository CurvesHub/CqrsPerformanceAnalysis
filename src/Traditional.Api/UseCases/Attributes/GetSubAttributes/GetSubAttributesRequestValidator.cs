using System.Globalization;
using FluentValidation;
using JetBrains.Annotations;
using Traditional.Api.Common.BaseRequests;

namespace Traditional.Api.UseCases.Attributes.GetSubAttributes;

/// <summary>
/// Defines the validation rules for the <see cref="GetSubAttributesRequest"/>.
/// </summary>
[UsedImplicitly]
public class GetSubAttributesRequestValidator
    : AbstractValidator<GetSubAttributesRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetSubAttributesRequestValidator"/> class.
    /// Defines the validation rules for the <see cref="GetSubAttributesRequest"/>.
    /// </summary>
    /// <param name="baseValidator">The validator for the <see cref="BaseRequest"/> class.</param>
    public GetSubAttributesRequestValidator(IValidator<BaseRequest> baseValidator)
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
