using System.Net;
using PerformanceTests.Common.Constants;
using PerformanceTests.Common.Models;
using PerformanceTests.Common.Services;
using Traditional.Api.Common.Endpoints;

namespace PerformanceTests.Endpoints.AutomatedTesting.CategoriesTests;

/// <inheritdoc />
public class GetChildrenOrTopLevelK6TestEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("K6Tests/categories/getChildrenOrTopLevel", StartK6TestForGetChildrenOrTopLevelAsync)
            .WithTags(EndpointTags.AUTOMATED_TESTING)
            .WithSummary("Automated K6 test for the GetChildrenOrTopLevel endpoint.")
            .Produces((int)HttpStatusCode.OK)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .WithOpenApi();
    }

    private static async Task<IResult> StartK6TestForGetChildrenOrTopLevelAsync(
        K6TestHandler handler,
        CancellationToken cancellationToken,
        AvailableApiNames apiToUse = AvailableApiNames.TraditionalApi,
        bool checkElastic = true,
        bool withWarmUp = true,
        bool saveMinimalResults = true,
        string seed = "hardcoded_seed")
    {
        var testInfo = TestInformation.CreateInfoForGetChildrenOrTopLevel(
            apiToUse,
            checkElastic,
            withWarmUp,
            saveMinimalResults,
            seed);

        await handler.StartK6TestAndProcessResultsAsync(testInfo, cancellationToken);
        return Results.Ok();
    }
}
