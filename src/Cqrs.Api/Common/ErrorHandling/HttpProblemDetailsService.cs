using System.Net;
using ErrorOr;
using ILogger = Serilog.ILogger;

namespace Cqrs.Api.Common.ErrorHandling;

/// <summary>
/// Logs errors and returns a problem details response.
/// </summary>
public class HttpProblemDetailsService(ILogger _logger)
{
    /// <summary>
    /// Logs the errors and returns a problem details response.
    /// </summary>
    /// <param name="errors">The errors to log.</param>
    /// <returns>The problem details response.</returns>
    public IResult LogErrorsAndReturnProblem(List<Error> errors)
    {
        if (errors.Count == 0)
        {
            _logger.Error("An error occurred, but no errors were provided");
            return Results.Problem(statusCode: 500);
        }

        _logger
            .ForContext("errors", errors, destructureObjects: true)
            .Error("{ErrorCount} error(s) occurred", errors.Count);

        var firstError = errors[0];
        var statusCode = firstError.Type switch
        {
            ErrorType.Failure => HttpStatusCode.InternalServerError,
            ErrorType.Unexpected => HttpStatusCode.InternalServerError,
            ErrorType.Validation => HttpStatusCode.BadRequest,
            ErrorType.Conflict => HttpStatusCode.Conflict,
            ErrorType.NotFound => HttpStatusCode.NotFound,
            ErrorType.Unauthorized => HttpStatusCode.Unauthorized,
            ErrorType.Forbidden => HttpStatusCode.Forbidden,
            _ => HttpStatusCode.InternalServerError
        };

        return Results.Problem(
            statusCode: (int)statusCode,
            extensions: new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
            {
                {
                    // The select is necessary because otherwise the ErrorType enum would be displayed as integer
                    "errors", errors.Select(e => new
                    {
                        e.Code,
                        e.Description,
                        Type = e.Type.ToString(),
                        e.NumericType,
                        e.Metadata
                    })
                }
            });
    }
}
