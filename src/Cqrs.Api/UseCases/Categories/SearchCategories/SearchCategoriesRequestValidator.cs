using Cqrs.Api.Common.BaseRequests;
using FluentValidation;
using JetBrains.Annotations;

namespace Cqrs.Api.UseCases.Categories.SearchCategories;

/// <summary>
/// Defines the validation rules for the <see cref="SearchCategoriesRequest"/> class.
/// </summary>
[UsedImplicitly]
public class SearchCategoriesRequestValidator : AbstractValidator<SearchCategoriesRequest>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SearchCategoriesRequestValidator"/> class.
    /// </summary>
    /// <param name="baseValidator">The validator for the <see cref="BaseRequest"/> class.</param>
    public SearchCategoriesRequestValidator(IValidator<BaseRequest> baseValidator)
    {
        Include(baseValidator);

        // One of them must be set
        RuleFor(request => request)
            .Must(request =>
                !string.IsNullOrWhiteSpace(request.SearchTerm)
                || request.CategoryNumber is not null and not 0)
            .WithName("SearchTermOrCategoryNumber")
            .WithMessage("Either the search term or the category number must be set.");

        // Not both of them can be set
        RuleFor(request => request)
            .Must(request =>
                string.IsNullOrWhiteSpace(request.SearchTerm)
                || request.CategoryNumber is null or 0)
            .WithName("SearchTermAndCategoryNumber")
            .WithMessage("You can't set both the search term and category number.");
    }
}
