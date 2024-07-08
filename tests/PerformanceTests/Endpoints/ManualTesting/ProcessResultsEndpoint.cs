using System.Net;
using PerformanceTests.Common.Constants;
using PerformanceTests.Common.Services;
using Traditional.Api.Common.Endpoints;

namespace PerformanceTests.Endpoints.ManualTesting;

/// <inheritdoc />
public class ProcessResultsEndpoint : IEndpoint
{
    /// <inheritdoc/>
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("manualTesting/processResults", ProcessResultsAsync)
            .WithTags(EndpointTags.MANUAL_TESTING)
            .WithSummary("Processes and saves all results of k6 tests.")
            .Produces((int)HttpStatusCode.OK)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .WithOpenApi();
    }

    private static async Task<IResult> ProcessResultsAsync(ResultProcessor processor)
    {
        await processor.ProcessResultsAsync();
        return Results.Ok();
    }
}
