using Cqrs.Api.Common.BaseRequests;
using FluentValidation;
using JetBrains.Annotations;

namespace Cqrs.Api.UseCases.Categories.Queries.GetChildrenOrTopLevel;

/// <summary>
/// Defines the validation rules for the <see cref="GetChildrenOrTopLevelRequest"/> class.
/// </summary>
[UsedImplicitly]
public class GetChildrenOrTopLevelRequestValidator : AbstractValidator<GetChildrenOrTopLevelRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetChildrenOrTopLevelRequestValidator"/> class.
    /// </summary>
    /// <param name="baseValidator">The validator for the <see cref="BaseRequest"/> class.</param>
    /// <remarks>
    /// Since the <see cref="GetChildrenOrTopLevelRequest.CategoryNumber"/> is nullable, we only need to validate the base request.
    /// </remarks>
    public GetChildrenOrTopLevelRequestValidator(IValidator<BaseRequest> baseValidator)
    {
        Include(baseValidator);
    }
}
