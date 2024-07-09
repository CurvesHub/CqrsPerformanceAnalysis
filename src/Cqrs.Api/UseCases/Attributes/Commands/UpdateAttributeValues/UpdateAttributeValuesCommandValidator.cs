using System.Diagnostics.CodeAnalysis;
using Cqrs.Api.Common.BaseRequests;
using FluentValidation;
using JetBrains.Annotations;

namespace Cqrs.Api.UseCases.Attributes.Commands.UpdateAttributeValues;

/// <summary>
/// Defines the validation rules for the <see cref="UpdateAttributeValuesCommand"/>.
/// </summary>
[UsedImplicitly]
[SuppressMessage("ReSharper", "ConditionalAccessQualifierIsNonNullableAccordingToAPIContract", Justification = "False Positive, when the request does not include the property, it is null.")]
public class UpdateAttributeValuesCommandValidator : AbstractValidator<UpdateAttributeValuesCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateAttributeValuesCommandValidator"/> class.
    /// Defines the validation rules for the <see cref="UpdateAttributeValuesCommand"/>.
    /// </summary>
    /// <param name="baseValidator">The validator for the <see cref="BaseQuery"/> class.</param>
    public UpdateAttributeValuesCommandValidator(IValidator<BaseQuery> baseValidator)
    {
        Include(baseValidator);

        RuleFor(request => request.NewAttributeValues)
            .NotEmpty()
            .WithMessage("The value of 'New Attribute Values' must not be empty.");

        RuleFor(request => request.NewAttributeValues)
            .Must(values => Array.TrueForAll(values, value => value.AttributeId > 0))
            .WithMessage("The value of 'New Attribute Values' -> 'Attribute Id' must be greater than '0'.");

        RuleFor(request => request.NewAttributeValues)
            .Must(values => Array.TrueForAll(values, value => value.InnerValues?.Count > 0))
            .WithMessage("The value of 'New Attribute Values' -> 'Inner Values' must not be empty.");

        RuleFor(request => request.NewAttributeValues)
            .Must(values => Array.TrueForAll(values, value =>
                value.InnerValues?.TrueForAll(innerValue => innerValue.CharacteristicId >= 0) == true))
            .WithMessage("The value of 'New Attribute Values' -> 'Inner Values' -> 'Characteristic Id' must be greater than or equal to '0'.");

        RuleFor(request => request.NewAttributeValues)
            .Must(values => Array.TrueForAll(values, value =>
                value.InnerValues?.TrueForAll(innerValue => innerValue.Values?.Length > 0) == true))
            .WithMessage("The value of 'New Attribute Values' -> 'Inner Values' -> 'Values' must not be empty.");
    }
}
