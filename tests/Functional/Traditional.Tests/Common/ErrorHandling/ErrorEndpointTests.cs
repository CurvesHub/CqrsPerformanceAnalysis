using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Testing;
using Traditional.Api.Common.ErrorHandling;

namespace Traditional.Tests.Common.ErrorHandling;

public class ErrorEndpointTests
{
    [Fact]
    public async Task GetErrorEndpoint_WhenCalledInProduction_ShouldReturnProblemDetailsWithStatus500()
    {
        // Arrange
        const string statusCode500Title = "An error occurred while processing your request.";

        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");
        await using var prodFactory = new WebApplicationFactory<Traditional.Api.Program>();

        // Act
        var response = await prodFactory.CreateClient().GetAsync(ErrorEndpoint.ErrorRoute);

        // Assert
        await ValidateResponse(response, HttpStatusCode.InternalServerError, statusCode500Title);
    }

    [Fact]
    public async Task GetErrorEndpoint_WhenCalledInDevelopment_ShouldReturnNotfound()
    {
        // Arrange
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Development");
        await using var devFactory = new WebApplicationFactory<Traditional.Api.Program>();

        // Act
        var response = await devFactory.CreateClient().GetAsync(ErrorEndpoint.ErrorRoute);

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
        response.ReasonPhrase.Should().Be("Not Found");
    }

    private static async Task ValidateResponse(
        HttpResponseMessage response,
        HttpStatusCode expectedStatusCode,
        string expectedTitle)
    {
        response.StatusCode.Should().Be(expectedStatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>();

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be((int)expectedStatusCode);
        problemDetails.Title.Should().Be(expectedTitle);

        // Currently not used in the application
        problemDetails.Detail.Should().Be("If the problem persists, please contact the maintainer of this service and provide the traceId.");
        problemDetails.Instance.Should().BeNullOrWhiteSpace();

        problemDetails.Extensions.Should().ContainKey("traceIdentifier");
        problemDetails.Extensions["traceIdentifier"]?.ToString().Should().NotBeNullOrEmpty();
    }
}
