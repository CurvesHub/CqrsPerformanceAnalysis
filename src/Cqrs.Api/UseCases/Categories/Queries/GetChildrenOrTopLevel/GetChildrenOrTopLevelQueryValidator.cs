using Cqrs.Api.Common.BaseRequests;
using FluentValidation;
using JetBrains.Annotations;

namespace Cqrs.Api.UseCases.Categories.Queries.GetChildrenOrTopLevel;

/// <summary>
/// Defines the validation rules for the <see cref="GetChildrenOrTopLevelQuery"/> class.
/// </summary>
[UsedImplicitly]
public class GetChildrenOrTopLevelQueryValidator : AbstractValidator<GetChildrenOrTopLevelQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="GetChildrenOrTopLevelQueryValidator"/> class.
    /// </summary>
    /// <param name="baseValidator">The validator for the <see cref="BaseRequest"/> class.</param>
    /// <remarks>
    /// Since the <see cref="GetChildrenOrTopLevelQuery.CategoryNumber"/> is nullable, we only need to validate the base request.
    /// </remarks>
    public GetChildrenOrTopLevelQueryValidator(IValidator<BaseRequest> baseValidator)
    {
        Include(baseValidator);
    }
}
