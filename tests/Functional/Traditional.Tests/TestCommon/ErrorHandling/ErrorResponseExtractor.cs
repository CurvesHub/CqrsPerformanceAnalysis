using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using ErrorOr;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Traditional.Tests.TestCommon.JsonConverter;

namespace Traditional.Tests.TestCommon.ErrorHandling;

/// <summary>
/// Provides methods for extracting errors from an error response.
/// </summary>
/// <typeparam name="TRequest">The type of the request in the metadata dictionary.</typeparam>
[SuppressMessage("StyleCop.CSharp.SpacingRules", "SA1011:Closing square brackets should be spaced correctly", Justification = "False positive.")]
public static class ErrorResponseExtractor<TRequest>
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new() { Converters = { new ErrorJsonConverter<TRequest>() } };

    /// <summary>
    /// Validates the response and extracts the errors.
    /// </summary>
    /// <param name="response">The response to validate.</param>
    /// <param name="expectedStatusCode">The expected status code.</param>
    /// <returns>An array of <see cref="ErrorOr.Error"/>s extracted from the response.</returns>
    public static async Task<Error[]> ValidateResponseAndGetErrorsAsync(
        HttpResponseMessage response,
        HttpStatusCode expectedStatusCode)
    {
        response.StatusCode.Should().Be(expectedStatusCode);

        var problemDetails = await response.Content.ReadFromJsonAsync<ProblemDetails>(JsonSerializerOptions);

        problemDetails.Should().NotBeNull();
        problemDetails!.Status.Should().Be((int)expectedStatusCode);

        // ReSharper disable once SwitchExpressionHandlesSomeKnownEnumValuesWithExceptionInDefault
        var expectedTitle = expectedStatusCode switch
        {
            HttpStatusCode.BadRequest => "Bad Request",
            HttpStatusCode.NotFound => "Not Found",
            HttpStatusCode.InternalServerError => "Internal Server Error",
            _ => throw new ArgumentOutOfRangeException(nameof(expectedStatusCode), expectedStatusCode, "Unexpected status code. Please define it in the switch expression.")
        };
        problemDetails.Title.Should().Be(expectedTitle);

        // Currently not used in the application
        problemDetails.Detail.Should().BeNullOrWhiteSpace();
        problemDetails.Instance.Should().BeNullOrWhiteSpace();

        // Errors
        problemDetails.Extensions.Should().NotBeNull().And.ContainKey("errors");
        var errorString = problemDetails.Extensions["errors"]!.ToString()!;

        var errors = JsonSerializer.Deserialize<Error[]>(errorString, JsonSerializerOptions)!;
        errors.Should().NotBeNull();

        return errors;
    }
}
