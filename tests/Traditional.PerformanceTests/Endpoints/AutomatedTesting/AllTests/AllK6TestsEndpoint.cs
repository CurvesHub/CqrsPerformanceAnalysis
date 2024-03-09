using System.Net;
using Traditional.Api.Common.Endpoints;
using Traditional.PerformanceTests.Endpoints.Common.Models;
using Traditional.PerformanceTests.Endpoints.Common.Services;
using EndpointTags = Traditional.PerformanceTests.Common.Constants.EndpointTags;

namespace Traditional.PerformanceTests.Endpoints.AutomatedTesting.AllTests;

/// <inheritdoc />
public class AllK6TestsEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("K6Tests/all", StartAllK6TestAsync)
            .WithTags(EndpointTags.ALL_TESTS)
            .WithSummary("Starts all automated K6 tests.")
            .Produces((int)HttpStatusCode.OK)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .WithOpenApi();
    }

    private static async Task<IResult> StartAllK6TestAsync(
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken,
        bool checkElastic = true,
        bool withWarmUp = true,
        bool saveMinimalResults = true)
    {
        List<TestInformation> testInfos =
        [
            TestInformation.CreateInfoForGetAttributes(checkElastic, withWarmUp, saveMinimalResults),
            TestInformation.CreateInfoForGetLeafAttributes(checkElastic, withWarmUp, saveMinimalResults),
            TestInformation.CreateInfoForGetSubAttributes(checkElastic, withWarmUp, saveMinimalResults),
            TestInformation.CreateInfoForUpdateAttributeValues(checkElastic, withWarmUp, saveMinimalResults),
            TestInformation.CreateInfoForGetCategoryMapping(checkElastic, withWarmUp, saveMinimalResults),
            TestInformation.CreateInfoForGetChildrenOrTopLevel(checkElastic, withWarmUp, saveMinimalResults),
            TestInformation.CreateInfoForSearchCategories(checkElastic, withWarmUp, saveMinimalResults),
            TestInformation.CreateInfoForUpdateCategoryMapping(checkElastic, withWarmUp, saveMinimalResults),
            TestInformation.CreateInfoForGetRootCategories(checkElastic, withWarmUp, saveMinimalResults)
        ];

        foreach (var testInfo in testInfos)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<K6TestHandler>();
            await handler.StartK6TestAndProcessResultsAsync(testInfo, cancellationToken);
        }

        return Results.Ok();
    }
}
