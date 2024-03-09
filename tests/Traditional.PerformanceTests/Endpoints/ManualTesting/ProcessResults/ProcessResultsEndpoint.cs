using System.Net;
using Traditional.Api.Common.Endpoints;
using Traditional.PerformanceTests.Endpoints.Common.Services;
using EndpointTags = Traditional.PerformanceTests.Common.Constants.EndpointTags;

namespace Traditional.PerformanceTests.Endpoints.ManualTesting.ProcessResults;

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
