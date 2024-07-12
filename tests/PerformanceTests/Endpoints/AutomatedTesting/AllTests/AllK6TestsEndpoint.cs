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
            .MapGet("K6Tests/allOfOneApi", StartAllK6TestByApiAsync)
            .WithTags(EndpointTags.ALL_TESTS)
            .WithSummary("Starts all automated K6 tests of either the traditional or the cqrs API or both.")
            .Produces((int)HttpStatusCode.OK)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .WithOpenApi();

        endpoints
            .MapGet("K6Tests/finialTestRunWithThreeSeeds", StartAllFinalK6TestAsync)
            .WithTags(EndpointTags.ALL_TESTS)
            .WithSummary("Starts all automated K6 tests of both the traditional and the cqrs API in sequence and with three different seeds.")
            .Produces((int)HttpStatusCode.OK)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .WithOpenApi();
    }

    private static async Task<IResult> StartAllK6TestByApiAsync(
        ILogger logger,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken,
        AvailableApiNames apiToUse = AvailableApiNames.AllApis,
        bool checkElastic = true,
        bool withWarmUp = true,
        bool saveMinimalResults = true,
        string seed = "hardcoded_seed")
    {
        List<TestInformation> testInfos = [];

        if (apiToUse == AvailableApiNames.AllApis)
        {
            testInfos.AddRange(GetAllTestInfosPerEndpointForApi(AvailableApiNames.TraditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed));
            testInfos.AddRange(GetAllTestInfosPerEndpointForApi(AvailableApiNames.CqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed));
            testInfos.AddRange(GetAllTestInfosPerEndpointForApi(AvailableApiNames.CqrsApiMediatr, checkElastic, withWarmUp, saveMinimalResults, seed));
        }
        else
        {
            testInfos.AddRange(GetAllTestInfosPerEndpointForApi(apiToUse, checkElastic, withWarmUp, saveMinimalResults, seed));
        }

        LogEstimatedRuntime(logger, testInfos);

        int testCount = 1;
        foreach (var testInfo in testInfos)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<K6TestHandler>();
            await handler.StartK6TestAndProcessResultsAsync(testInfo, cancellationToken);
            logger.Information("Handled test {CurrentTestCount} of {TotalTestCount}", testCount++, testInfos.Count);
        }

        return Results.Ok();
    }

    private static async Task<IResult> StartAllFinalK6TestAsync(
        ILogger logger,
        IServiceProvider serviceProvider,
        CancellationToken cancellationToken,
        int testsPerApi = 1)
    {
        const bool checkElastic = true, withWarmUp = true, saveMinimalResults = true;
        string[] seeds = ["hardcoded_seed", "another_seed", "third_seed"];

        // Add 3 APIs with 9 endpoint tests for 3 seeds -> 3*9*3 = 81 Tests a 2min = 162min = 2h 42min
        List<TestInformation> testInfos = [];
        foreach (var seed in seeds)
        {
            testInfos.AddRange(GetAllTestInfosPerEndpointForApi(AvailableApiNames.TraditionalApi, checkElastic, withWarmUp, saveMinimalResults, seed));
            testInfos.AddRange(GetAllTestInfosPerEndpointForApi(AvailableApiNames.CqrsApi, checkElastic, withWarmUp, saveMinimalResults, seed));
            testInfos.AddRange(GetAllTestInfosPerEndpointForApi(AvailableApiNames.CqrsApiMediatr, checkElastic, withWarmUp, saveMinimalResults, seed));
        }

        if (testsPerApi > 1)
        {
            // Copy the testInfos for each additional test
            var additionalTestInfos = new List<TestInformation>(testInfos);
            for (int i = 1; i < testsPerApi; i++)
            {
                testInfos.AddRange(additionalTestInfos);
            }
        }

        LogEstimatedRuntime(logger, testInfos);

        int testCount = 1;
        foreach (var testInfo in testInfos)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var handler = scope.ServiceProvider.GetRequiredService<K6TestHandler>();
            await handler.StartK6TestAndProcessResultsAsync(testInfo, cancellationToken);
            logger.Information("Handled test {CurrentTestCount} of {TotalTestCount}", testCount++, testInfos.Count);
        }

        return Results.Ok();
    }

    private static List<TestInformation> GetAllTestInfosPerEndpointForApi(
        AvailableApiNames apiToUse,
        bool checkElastic,
        bool withWarmUp,
        bool saveMinimalResults,
        string seed)
    {
        return
        [
            TestInformation.CreateInfoForGetAttributes(apiToUse, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForGetLeafAttributes(apiToUse, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForGetSubAttributes(apiToUse, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForUpdateAttributeValues(apiToUse, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForGetCategoryMapping(apiToUse, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForGetChildrenOrTopLevel(apiToUse, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForSearchCategories(apiToUse, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForUpdateCategoryMapping(apiToUse, checkElastic, withWarmUp, saveMinimalResults, seed),
            TestInformation.CreateInfoForGetRootCategories(apiToUse, checkElastic, withWarmUp, saveMinimalResults, seed)
        ];
    }

    private static void LogEstimatedRuntime(ILogger logger, List<TestInformation> testInfos)
    {
        var estimatedRuntime5S = TimeSpan.FromSeconds(testInfos.Count * 5);
        var estimatedRuntime1M = TimeSpan.FromMinutes(testInfos.Count * 1);
        var estimatedRuntime2M = TimeSpan.FromMinutes(testInfos.Count * 2);

        logger.Information(
            "Starting {TestCount} tests with estimated runtime (5s | 1min | 2min per Test) of {EstimatedRuntime5s} min | {EstimatedRuntime1m} min | {EstimatedRuntime2m} min\n" +
            "In hours: {EstimatedRuntime5sHours} hours | {EstimatedRuntime1mHours} hours | {EstimatedRuntime2mHours} hours",
            testInfos.Count,
            estimatedRuntime5S.TotalMinutes,
            estimatedRuntime1M.TotalMinutes,
            estimatedRuntime2M.TotalMinutes,
            estimatedRuntime5S.TotalHours,
            estimatedRuntime1M.TotalHours,
            estimatedRuntime2M.TotalHours);
    }
}
