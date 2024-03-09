using Microsoft.AspNetCore.Mvc;
using Traditional.Api.Common.Endpoints;
using Traditional.PerformanceTests.Endpoints.Common.Services;
using EndpointTags = Traditional.PerformanceTests.Common.Constants.EndpointTags;

namespace Traditional.PerformanceTests.Endpoints.ManualTesting.SetupTestData;

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
