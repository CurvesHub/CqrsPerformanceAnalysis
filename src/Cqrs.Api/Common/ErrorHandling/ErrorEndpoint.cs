using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using ILogger = Serilog.ILogger;

namespace Cqrs.Api.Common.ErrorHandling;

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
    /// </summary>
    /// <remarks>
    /// This endpoint returns a <see cref="ProblemDetails"/> response
    /// with a status code of 500 and a traceId.
    /// <list type="bullet">
    /// <item>It logs the exception and the request details with the traceId.</item>
    /// <item>It is used by the exception handling middleware in the request pipeline.</item>
    /// </list>
    /// </remarks>
    /// <param name="endpoints">The endpoint route builder.</param>
    public static void MapErrorEndpoint(this IEndpointRouteBuilder endpoints)
    {
        endpoints.Map(
            ErrorRoute,
            (HttpContext context, [FromServices] ILogger logger) =>
        {
            var exception = context.Features.Get<IExceptionHandlerFeature>()?.Error;

            logger
                .ForContext("RequestHeaders", context.Request.Headers)
                .ForContext("RequestMethod", context.Request.Method)
                .ForContext("RequestBody", context.Request.Body)
                .ForContext("RequestQueryString", context.Request.QueryString)
                .ForContext("RequestRouteValues", context.Request.RouteValues)
                .ForContext("RequestPathBase", context.Request.PathBase)
                .ForContext("RequestPath", context.Request.Path)
                .ForContext("TraceIdentifier", context.TraceIdentifier)
                .Error(exception, "An exception occurred!");

            return Results.Problem(
                detail: "If the problem persists, please contact the maintainer of this service and provide the traceId.",
                statusCode: 500,
                extensions: new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
                {
                    { "traceIdentifier", context.TraceIdentifier }
                });
        });
    }
}
