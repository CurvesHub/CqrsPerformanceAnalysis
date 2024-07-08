using System.Diagnostics.CodeAnalysis;
using System.Net;
using PerformanceTests.Common.Constants;
using PerformanceTests.Common.Models;
using PerformanceTests.Common.Services;
using Traditional.Api.Common.Endpoints;
using ILogger = Serilog.ILogger;

namespace PerformanceTests.Endpoints.AutomatedTesting.AllTests;

/// <inheritdoc />
[SuppressMessage("SonarLint", "S107", Justification = "This test requires a lot of parameters to be passed in the query string.")]
public class AllK6TestsEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("K6Tests/allOfBothApis", StartAllK6TestAsync)
            .WithTags(EndpointTags.ALL_TESTS)
            .WithSummary("Starts all automated K6 tests of both the traditional and the cqrs API in sequence.")
            .Produces((int)HttpStatusCode.OK)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .WithOpenApi();

        endpoints
            .MapGet("K6Tests/allOfOneApi", StartAllK6TestByApiAsync)
            .WithTags(EndpointTags.ALL_TESTS)
            .WithSummary("Starts all automated K6 tests of either the traditional or the cqrs API.")
            .Produces((int)HttpStatusCode.OK)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .WithOpenApi();

        endpoints
            .MapGet("K6Tests/finialTestRunWithThreeSeeds", StartAllFinalK6TestAsync)
            .WithTags(EndpointTags.ALL_TESTS)
            .WithSummary("Starts all automated K6 tests of both the traditional and the cqrs API in sequence. With three different seeds.")
            .Produces((int)HttpStatusCode.OK)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .WithOpenApi();
    }

    private static async Task<IResult> StartAllK6TestByApiAsync(
        ILogger logger,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken,
        bool useTraditionalApi = true,
        bool checkElastic = true,
        bool withWarmUp = true,
        bool saveMinimalResults = true,
        string seed = "hardcoded_seed")
    {
        List<TestInformation> testInfos =
        [
            TestInformation.CreateInfoForGetAttributes(useTraditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForGetLeafAttributes(useTraditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForGetSubAttributes(useTraditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForUpdateAttributeValues(useTraditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForGetCategoryMapping(useTraditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForGetChildrenOrTopLevel(useTraditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForSearchCategories(useTraditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForUpdateCategoryMapping(useTraditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForGetRootCategories(useTraditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed)
        ];

        int totalTestCount = testInfos.Count;
        int currentTestCount = 1;
        foreach (var testInfo in testInfos)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<K6TestHandler>();
            await handler.StartK6TestAndProcessResultsAsync(testInfo, cancellationToken);
            logger.Information("Handled test {CurrentTestCount} of {TotalTestCount}", currentTestCount++, totalTestCount);
        }

        return Results.Ok();
    }

    private static async Task<IResult> StartAllK6TestAsync(
        ILogger logger,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken,
        bool checkElastic = true,
        bool withWarmUp = true,
        bool saveMinimalResults = true,
        string seed = "hardcoded_seed")
    {
        const bool traditionalApi = true;
        const bool cqrsApi = false;
        List<TestInformation> testInfos =
        [
            TestInformation.CreateInfoForGetAttributes(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForGetAttributes(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed),

            TestInformation.CreateInfoForGetLeafAttributes(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForGetLeafAttributes(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed),

            TestInformation.CreateInfoForGetSubAttributes(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForGetSubAttributes(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed),

            TestInformation.CreateInfoForUpdateAttributeValues(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForUpdateAttributeValues(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed),

            TestInformation.CreateInfoForGetCategoryMapping(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForGetCategoryMapping(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed),

            TestInformation.CreateInfoForGetChildrenOrTopLevel(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForGetChildrenOrTopLevel(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed),

            TestInformation.CreateInfoForSearchCategories(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForSearchCategories(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed),

            TestInformation.CreateInfoForUpdateCategoryMapping(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForUpdateCategoryMapping(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed),

            TestInformation.CreateInfoForGetRootCategories(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForGetRootCategories(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed)
        ];

        int totalTestCount = testInfos.Count;
        int currentTestCount = 1;
        foreach (var testInfo in testInfos)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<K6TestHandler>();
            await handler.StartK6TestAndProcessResultsAsync(testInfo, cancellationToken);
            logger.Information("Handled test {CurrentTestCount} of {TotalTestCount}", currentTestCount++, totalTestCount);
        }

        return Results.Ok();
    }

    private static async Task<IResult> StartAllFinalK6TestAsync(
        ILogger logger,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken,
        bool checkElastic = true,
        bool withWarmUp = true,
        bool saveMinimalResults = true)
    {
        string[] seeds = ["hardcoded_seed", "another_seed", "third_seed"];
        const bool traditionalApi = true;
        const bool cqrsApi = false;

        List<TestInformation> testInfos = [];
        foreach (var seed in seeds)
        {
            // Add 10 tests for each seed
            for (int i = 0; i < 10; i++)
            {
                testInfos.AddRange(
                [
                    TestInformation.CreateInfoForGetAttributes(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
                    TestInformation.CreateInfoForGetAttributes(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed),

                    TestInformation.CreateInfoForGetLeafAttributes(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
                    TestInformation.CreateInfoForGetLeafAttributes(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed),

                    TestInformation.CreateInfoForGetSubAttributes(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
                    TestInformation.CreateInfoForGetSubAttributes(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed),

                    TestInformation.CreateInfoForUpdateAttributeValues(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
                    TestInformation.CreateInfoForUpdateAttributeValues(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed),

                    TestInformation.CreateInfoForGetCategoryMapping(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
                    TestInformation.CreateInfoForGetCategoryMapping(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed),

                    TestInformation.CreateInfoForGetChildrenOrTopLevel(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
                    TestInformation.CreateInfoForGetChildrenOrTopLevel(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed),

                    TestInformation.CreateInfoForSearchCategories(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
                    TestInformation.CreateInfoForSearchCategories(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed),

                    TestInformation.CreateInfoForUpdateCategoryMapping(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
                    TestInformation.CreateInfoForUpdateCategoryMapping(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed),

                    TestInformation.CreateInfoForGetRootCategories(traditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed),
                    TestInformation.CreateInfoForGetRootCategories(cqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed)
                ]);
            }
        }

        int totalTestCount = testInfos.Count;
        int currentTestCount = 1;
        foreach (var testInfo in testInfos.OrderBy(
                     testInfo => testInfo.EndpointName + (testInfo.UseTraditionalApi ? "Traditional" : "CQRS"),
                     StringComparer.InvariantCulture))
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<K6TestHandler>();
            await handler.StartK6TestAndProcessResultsAsync(testInfo, cancellationToken);
            logger.Information("Handled test {CurrentTestCount} of {TotalTestCount}", currentTestCount++, totalTestCount);
        }

        return Results.Ok();
    }
}
