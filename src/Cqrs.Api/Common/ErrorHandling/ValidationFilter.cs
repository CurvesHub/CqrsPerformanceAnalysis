using Cqrs.Api.Common.BaseRequests;
using Cqrs.Api.Common.DataAccess.Repositories;
using Cqrs.Api.UseCases.RootCategories.Common.Errors;
using Cqrs.Api.UseCases.RootCategories.Common.Persistence.Entities;
using ErrorOr;
using FluentValidation;

namespace Cqrs.Api.Common.ErrorHandling;

/// <summary>
/// Defines a validation filter which validates a <see cref="BaseRequest"/>.
/// </summary>
/// <param name="_requestValidator">The request validator.</param>
/// <param name="_rootCategoryReadRepository">The root category repository.</param>
/// <typeparam name="TRequest">The type of the request.</typeparam>
public class ValidationFilter<TRequest>(
    IValidator<TRequest> _requestValidator,
    ICachedReadRepository<RootCategory> _rootCategoryReadRepository)
    : IEndpointFilter
    where TRequest : BaseRequest
{
    /// <inheritdoc />
    /// <remarks>
    /// This method checks if the request is valid and if the root category exists before the endpoint is executed.
    /// </remarks>
    public async ValueTask<object?> InvokeAsync(
        EndpointFilterInvocationContext context,
        EndpointFilterDelegate next)
    {
        var request = context.GetArgument<TRequest>(0);
        var validationResult = await _requestValidator.ValidateAsync(request, context.HttpContext.RequestAborted);

        List<Error>? errors = null;
        if (!validationResult.IsValid)
        {
            errors = validationResult.Errors.ConvertAll(error =>
                Error.Validation(
                    code: error.PropertyName,
                    description: error.ErrorMessage));
        }

        if (errors is not null)
        {
            return context.HttpContext.RequestServices.GetRequiredService<HttpProblemDetailsService>()
                .LogErrorsAndReturnProblem(errors);
        }

        if (await _rootCategoryReadRepository.GetByIdAsync(request.RootCategoryId) is null)
        {
            return context.HttpContext.RequestServices.GetRequiredService<HttpProblemDetailsService>()
                .LogErrorsAndReturnProblem([RootCategoryErrors.RootCategoryIdNotFound(request.RootCategoryId)]);
        }

        return await next(context);
    }
}
