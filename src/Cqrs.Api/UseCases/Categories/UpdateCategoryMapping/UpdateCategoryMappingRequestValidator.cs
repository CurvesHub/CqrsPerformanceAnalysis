using Cqrs.Api.Common.BaseRequests;
using FluentValidation;
using JetBrains.Annotations;

namespace Cqrs.Api.UseCases.Categories.UpdateCategoryMapping;

/// <summary>
/// Defines the validation rules for the <see cref="UpdateCategoryMappingRequest"/> class.
/// </summary>
[UsedImplicitly]
public class UpdateCategoryMappingRequestValidator : AbstractValidator<UpdateCategoryMappingRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCategoryMappingRequestValidator"/> class.
    /// </summary>
    /// <param name="baseValidator">The validator for the <see cref="BaseRequest"/> class.</param>
    public UpdateCategoryMappingRequestValidator(IValidator<BaseRequest> baseValidator)
    {
        Include(baseValidator);

        RuleFor(x => x.CategoryNumber)
            .GreaterThan(0)
            .WithMessage("The value of 'Category Number' must be greater than '0'.");
    }
}
