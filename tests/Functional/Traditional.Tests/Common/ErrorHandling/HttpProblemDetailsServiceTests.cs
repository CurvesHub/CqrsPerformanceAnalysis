using System.Net;
using ErrorOr;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Traditional.Api.Common.ErrorHandling;
using Traditional.Tests.TestCommon.Logging;
using Xunit.Abstractions;

namespace Traditional.Tests.Common.ErrorHandling;

public class HttpProblemDetailsServiceTests(ITestOutputHelper outputHelper)
{
    public static readonly TheoryData<Error, HttpStatusCode> ErrorToStatusCode = new()
    {
        { Error.Failure(), HttpStatusCode.InternalServerError },
        { Error.Unexpected(), HttpStatusCode.InternalServerError },
        { Error.Validation(), HttpStatusCode.BadRequest },
        { Error.Conflict(), HttpStatusCode.Conflict },
        { Error.NotFound(), HttpStatusCode.NotFound },
        { Error.Unauthorized(), HttpStatusCode.Unauthorized },
        { Error.Forbidden(), HttpStatusCode.Forbidden },
        { Error.Custom(99, "CustomCode", "CustomDescription"), HttpStatusCode.InternalServerError }
    };

    public static readonly TheoryData<List<Error>, HttpStatusCode> MultipleErrorsToStatusCode = new()
    {
        { [Error.Failure(), Error.Validation()], HttpStatusCode.InternalServerError },
        { [Error.Validation(), Error.NotFound()], HttpStatusCode.BadRequest },
        { [Error.Unauthorized(), Error.NotFound()], HttpStatusCode.Unauthorized }
    };

    private readonly HttpProblemDetailsService _service = new(TestLogger.CreateWithNewContext(outputHelper));

    [Fact]
    public void LogErrorsAndReturnProblem_WhenCalledWithEmptyList_ShouldReturnProblemDetailsWithStatusCode500()
    {
        // Arrange
        var emptyErrors = new List<Error>();

        // Act
        var result = (ProblemHttpResult)_service.LogErrorsAndReturnProblem(emptyErrors);

        // Assert
        result.Should().BeEquivalentTo((ProblemHttpResult)Results.Problem(statusCode: 500));
    }

    [Theory]
    [MemberData(nameof(ErrorToStatusCode))]
    public void LogErrorsAndReturnProblem_WhenCalledWithDifferentErrorType_ShouldReturnProblemDetailsWithExpectedStatusCode(
        Error error,
        HttpStatusCode expectedStatusCode)
    {
        // Act
        var result = (ProblemHttpResult)_service.LogErrorsAndReturnProblem([error]);

        // Assert
        result.StatusCode.Should().Be((int)expectedStatusCode);
    }

    [Theory]
    [MemberData(nameof(MultipleErrorsToStatusCode))]
    public void
        LogErrorsAndReturnProblem_WhenCalledWithMultipleErrors_ShouldReturnProblemDetailsWithAllErrorsAndStatusCodeFromFirstError(
            List<Error> errors,
            HttpStatusCode expectedStatusCode)
    {
        // Act
        var result = (ProblemHttpResult)_service.LogErrorsAndReturnProblem(errors);

        // Assert
        result.Should().BeEquivalentTo((ProblemHttpResult)Results.Problem(
            statusCode: (int)expectedStatusCode,
            extensions: new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            {
                nameof(errors), errors.Select(e => new
                {
                    e.Code,
                    e.Description,
                    Type = e.Type.ToString(),
                    e.NumericType,
                    e.Metadata
                })
            }
        }));
    }

    [Fact]
    public void LogErrorsAndReturnProblem_WhenCalledWithErrors_ShouldLogErrors()
    {
        // Arrange
        List<Error> errors = [Error.Failure(), Error.Validation(), Error.Conflict()];

        // Act
        var result = (ProblemHttpResult)_service.LogErrorsAndReturnProblem(errors);

        // Assert
        result.StatusCode.Should().Be(500);
        TestLogger.HasLogEventCountEqualTo(1);
        TestLogger.HasLogEventWithError();
        TestLogger.HasLogEventWithTemplateEqualTo("{ErrorCount} error(s) occurred");
        TestLogger.HasLogEventWithPropertyScalarValueEqualTo("ErrorCount", errors.Count);
        TestLogger.HasLogEventWithPropertyWithListOfErrorsEqualTo("errors", errors);
    }
}
