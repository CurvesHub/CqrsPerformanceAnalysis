using Microsoft.AspNetCore.Mvc;
using PerformanceTests.Common.Constants;
using PerformanceTests.Common.Services;
using Traditional.Api.Common.Endpoints;

namespace PerformanceTests.Endpoints.ManualTesting;

/// <inheritdoc/>
public class SetupTestDataEndpoint : IEndpoint
{
    /// <inheritdoc/>
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("manualTesting/addExampleData", SetupExampleDataAsync)
            .WithTags(EndpointTags.MANUAL_TESTING);
    }

    private static async Task<IResult> SetupExampleDataAsync(TestDataGenerator testDataGenerator, CancellationToken cancellationToken, [FromQuery] int dataCount = 10_000)
    {
        await testDataGenerator.SetupExampleDataAsync(dataCount, cancellationToken);
        return Results.Ok();
    }
}
