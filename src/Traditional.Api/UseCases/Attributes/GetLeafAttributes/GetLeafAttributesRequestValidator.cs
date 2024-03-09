using System.Globalization;
using FluentValidation;
using JetBrains.Annotations;
using Traditional.Api.Common.BaseRequests;

namespace Traditional.Api.UseCases.Attributes.GetLeafAttributes;

/// <summary>
/// Defines the validation rules for the <see cref="GetLeafAttributesRequest"/>.
/// </summary>
[UsedImplicitly]
public class GetLeafAttributesRequestValidator
    : AbstractValidator<GetLeafAttributesRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetLeafAttributesRequestValidator"/> class.
    /// Defines the validation rules for the <see cref="GetLeafAttributesRequest"/>.
    /// </summary>
    /// <param name="baseValidator">The validator for the <see cref="BaseRequest"/> class.</param>
    public GetLeafAttributesRequestValidator(IValidator<BaseRequest> baseValidator)
    {
        Include(baseValidator);

        RuleFor(request => request.AttributeId)
            .Must(attributeId => int.TryParse(attributeId, NumberStyles.Integer, NumberFormatInfo.InvariantInfo, out int id) && id > 0)
            .WithMessage("The value of 'Attribute Id' must be greater than '0'.");
    }
}
