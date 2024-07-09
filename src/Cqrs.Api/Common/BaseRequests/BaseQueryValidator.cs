using System.Globalization;
using FluentValidation;
using JetBrains.Annotations;

namespace Cqrs.Api.Common.BaseRequests;

/// <summary>
/// Defines the validation rules for the <see cref="BaseQuery"/> class.
/// </summary>
[UsedImplicitly]
public class BaseQueryValidator : AbstractValidator<BaseQuery>
{
    /// <summary>
    /// Initializes a new instance of the <see cref="BaseQueryValidator"/> class.
    /// </summary>
    public BaseQueryValidator()
    {
        RuleFor(x => x.RootCategoryId)
            .GreaterThan(0)
            .WithMessage("The value of 'Root Category Id' must be greater than '0'.");

        RuleFor(x => x.ArticleNumber)
            .NotEmpty()
            .Must(articleNumber =>
                long.TryParse(
                    articleNumber,
                    NumberStyles.Integer,
                    NumberFormatInfo.InvariantInfo,
                    out long articleNumberLong)
                && articleNumberLong > 0)
            .WithMessage("The value of 'Article Number' must be greater than '0'.");
    }
}
