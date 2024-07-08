using System.Net;
using PerformanceTests.Common.Constants;
using PerformanceTests.Common.Models;
using PerformanceTests.Common.Services;
using Traditional.Api.Common.Endpoints;

namespace PerformanceTests.Endpoints.AutomatedTesting.RootCategoriesTests;

/// <inheritdoc />
public class GetRootCategoriesK6TestEndpoint : IEndpoint
{
    /// <inheritdoc/>
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("K6Tests/rootCategories/getRootCategories", StartK6TestForGetRootCategoriesAsync)
            .WithTags(EndpointTags.AUTOMATED_TESTING)
            .WithSummary("Automated K6 test for the GetRootCategories endpoint.")
            .Produces((int)HttpStatusCode.OK)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .WithOpenApi();
    }

    private static async Task<IResult> StartK6TestForGetRootCategoriesAsync(
        K6TestHandler handler,
        CancellationToken cancellationToken,
        bool useTraditionalApi = true,
        bool checkElastic = true,
        bool withWarmUp = true,
        bool saveMinimalResults = true,
        string seed = "hardcoded_seed")
    {
        var testInfo = TestInformation.CreateInfoForGetRootCategories(
            useTraditionalApi,
            checkElastic,
            withWarmUp,
            saveMinimalResults,
            seed);

        await handler.StartK6TestAndProcessResultsAsync(testInfo, cancellationToken);
        return Results.Ok();
    }
}
