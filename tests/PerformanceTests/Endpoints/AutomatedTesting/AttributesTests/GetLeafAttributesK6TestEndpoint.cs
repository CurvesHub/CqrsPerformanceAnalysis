using System.Net;
using PerformanceTests.Common.Constants;
using PerformanceTests.Common.Models;
using PerformanceTests.Common.Services;
using Traditional.Api.Common.Endpoints;

namespace PerformanceTests.Endpoints.AutomatedTesting.AttributesTests;

/// <inheritdoc />
public class GetLeafAttributesK6TestEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("K6Tests/attributes/getLeafAttributes", StartK6TestForGetLeafAttributesAsync)
            .WithTags(EndpointTags.AUTOMATED_TESTING)
            .WithSummary("Automated K6 test for the SearchCategories endpoint.")
            .Produces((int)HttpStatusCode.OK)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .WithOpenApi();
    }

    private static async Task<IResult> StartK6TestForGetLeafAttributesAsync(
        K6TestHandler handler,
        CancellationToken cancellationToken,
        bool useTraditionalApi = true,
        bool checkElastic = true,
        bool withWarmUp = true,
        bool saveMinimalResults = true,
        string seed = "hardcoded_seed")
    {
        var testInfo = TestInformation.CreateInfoForGetLeafAttributes(
            useTraditionalApi,
            checkElastic,
            withWarmUp,
            saveMinimalResults,
            seed);

        await handler.StartK6TestAndProcessResultsAsync(testInfo, cancellationToken);
        return Results.Ok();
    }
}
