using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ILogger = Serilog.ILogger;

namespace Traditional.PerformanceTests.Endpoints.Common.ErrorHandling;

/// <summary>
/// Maps the endpoint for handling exceptions.
/// </summary>
public static class ErrorEndpoint
{
    /// <summary>
    /// Gets the route of the endpoint.
    /// </summary>
    public static string ErrorRoute => "/error";

    /// <summary>
    /// Maps the error endpoint for exceptions that occur during the request processing.
    /// This endpoint returns a problem response with the exception message.
    /// It is used by the exception handling middleware in the request pipeline.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    public static void MapErrorEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.Map(ErrorRoute, (HttpContext context, [FromServices] ILogger logger) =>
        {
            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;
            logger.Error(exception, "An error occurred while running the K6 test");
            return Results.Problem(detail: exception?.Message, statusCode: 500);
        });
    }
}
