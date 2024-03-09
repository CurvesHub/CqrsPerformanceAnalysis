using System.Net;
using Traditional.Api.Common.Endpoints;
using Traditional.PerformanceTests.Endpoints.Common.Models;
using Traditional.PerformanceTests.Endpoints.Common.Services;
using EndpointTags = Traditional.PerformanceTests.Common.Constants.EndpointTags;

namespace Traditional.PerformanceTests.Endpoints.AutomatedTesting.CategoriesTests;

/// <inheritdoc />
public class UpdateCategoryMappingK6TestEndpoint : IEndpoint
{
    /// <inheritdoc />
    public void MapEndpoint(IEndpointRouteBuilder endpoints)
    {
        endpoints
            .MapGet("K6Tests/categories/updateCategoryMapping", StartK6TestForUpdateCategoryMappingAsync)
            .WithTags(EndpointTags.AUTOMATED_TESTING)
            .WithSummary("Automated K6 test for the UpdateCategoryMapping endpoint.")
            .Produces((int)HttpStatusCode.OK)
            .ProducesProblem((int)HttpStatusCode.InternalServerError)
            .WithOpenApi();
    }

    private static async Task<IResult> StartK6TestForUpdateCategoryMappingAsync(
        K6TestHandler handler,
        CancellationToken cancellationToken,
        bool checkElastic = true,
        bool withWarmUp = true,
        bool saveMinimalResults = true)
    {
        var testInfo = TestInformation.CreateInfoForUpdateCategoryMapping(
            checkElastic,
            withWarmUp,
            saveMinimalResults);

        await handler.StartK6TestAndProcessResultsAsync(testInfo, cancellationToken);
        return Results.Ok();
    }
}
