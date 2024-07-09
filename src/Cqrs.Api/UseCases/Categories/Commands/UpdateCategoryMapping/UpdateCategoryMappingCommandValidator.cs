using Cqrs.Api.Common.BaseRequests;
using FluentValidation;
using JetBrains.Annotations;

namespace Cqrs.Api.UseCases.Categories.Commands.UpdateCategoryMapping;

/// <summary>
/// Defines the validation rules for the <see cref="UpdateCategoryMappingCommand"/> class.
/// </summary>
[UsedImplicitly]
public class UpdateCategoryMappingCommandValidator : AbstractValidator<UpdateCategoryMappingCommand>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UpdateCategoryMappingCommandValidator"/> class.
    /// </summary>
    /// <param name="baseValidator">The validator for the <see cref="BaseRequest"/> class.</param>
    public UpdateCategoryMappingCommandValidator(IValidator<BaseRequest> baseValidator)
    {
        Include(baseValidator);

        RuleFor(x => x.CategoryNumber)
            .GreaterThan(0)
            .WithMessage("The value of 'Category Number' must be greater than '0'.");
    }
}
