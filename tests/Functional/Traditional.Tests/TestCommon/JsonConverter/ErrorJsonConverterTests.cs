using System.Text.Json;
using ErrorOr;
using FluentAssertions;
using JetBrains.Annotations;
using Traditional.Tests.TestCommon.ErrorHandling;

namespace Traditional.Tests.TestCommon.JsonConverter;

public class ErrorJsonConverterTests
{
    [UsedImplicitly(ImplicitUseTargetFlags.Members)]
    private record TestRequest(string Name, int Age, string? Email);

    private readonly JsonSerializerOptions _errorConverterOptions = new() { Converters = { new ErrorJsonConverter<TestRequest>() } };
    private readonly JsonSerializerOptions _camelCaseOptions = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    [Fact]
    public void Deserialize_WhenErrorJsonIsValid_ShouldDeserializeError()
    {
        // Arrange
        var request = new TestRequest(Name: "John Doe", Age: 30, Email: null);

        var error = Error.Validation(
            code: "ValidationError",
            description: "The request is not valid.",
            metadata: new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase) { { "request", request } });

        var errorString = JsonSerializer.Serialize(error, _camelCaseOptions);

        // Act
        var deserializedError = JsonSerializer.Deserialize<Error>(errorString, _errorConverterOptions);

        // Assert
        deserializedError.ShouldBeEquivalentTo(error);
        deserializedError.Metadata.Should().BeEquivalentTo(error.Metadata);
        deserializedError.Metadata!["request"].Should().BeEquivalentTo(error.Metadata!["request"]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(3)]
    [InlineData(5)]
    public void Deserialize_WhenForEachErrorInArrayOfErrorsJsonIsValid_ShouldDeserializeEachErrorInArrayOfErrors(int errorCount)
    {
        // Arrange
        var errors = Enumerable.Range(1, errorCount)
            .Select(i => Error.Validation(
                code: $"ValidationError{i}",
                description: $"The request {i} is not valid.",
                metadata: new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "request", new TestRequest(
                            Name: $"John Doe {i}",
                            Age: 30 + i,
                            Email: errorCount % i is 0 ? null : $"john{i}@example.com")
                    }
                }))
            .ToArray();

        var errorStrings = errors.Select(error => JsonSerializer.Serialize(error, _camelCaseOptions));

        // Act
        var deserializedErrors = errorStrings
            .Select(error => JsonSerializer.Deserialize<Error>(error, _errorConverterOptions))
            .ToArray();

        // Assert
        deserializedErrors.ShouldBeEquivalentTo(errors);
    }

    [Fact]
    public void Deserialize_WhenArrayOfErrorsJsonIsValid_ShouldDeserializeArrayOfErrors()
    {
        // Arrange
        var errors = Enumerable.Range(1, 3)
            .Select(i => Error.Validation(
                code: $"ValidationError{i}",
                description: $"The request {i} is not valid.",
                metadata: new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase)
                {
                    {
                        "request", new TestRequest(
                            Name: $"John Doe {i}",
                            Age: 30 + i,
                            Email: 3 % i is 0 ? null : $"john{i}@example.com")
                    }
                }))
            .ToArray();

        var errorString = JsonSerializer.Serialize(errors, _camelCaseOptions);

        // Act
        var deserializedErrors = JsonSerializer.Deserialize<Error[]>(errorString, _errorConverterOptions);

        // Assert
        deserializedErrors.Should().NotBeNull();
        deserializedErrors!.ShouldBeEquivalentTo(errors);
    }
}
